using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

//$ caching of tag lists

namespace FzCommon
{
    public interface ILogTaggable
    {
        // e.g. 'loc' or 'dev'
        string GetTagCategory();
        
        // e.g. Location.Id or Device.DeviceId, stringified
        string GetTagId();

        // e.g. 'Location: Snoqualmie River North Fork'
        string GetTagName();
    }

    public interface ILogTag
    {
        string GetTag();
        string GetTagName();
        ILogTaggable GetTaggable();
    }

    public interface ILogBookTaggableFactory
    {
        Task<List<ILogTaggable>> GetAvailableTaggables(SqlConnection sqlcn, string category);
    }

    public class LogBookEntry
    {
        public int Id;
        public int UserId;
        public DateTime Timestamp;
        public string Text;
        public bool IsDeleted;
        public List<string> Tags;

        public void Save()
        {
            this.ProcessCustomTags();
            
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                SqlCommand cmd = new SqlCommand("SaveLogBookEntry", sqlcn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", Id);
                cmd.Parameters.AddWithValue("@UserId", UserId);
                cmd.Parameters.AddWithValue("@Timestamp", Timestamp);
                cmd.Parameters.AddWithValue("@Text", Text);
                cmd.Parameters.AddWithValue("@IsDeleted", IsDeleted);
                if (Tags != null)
                {
                    cmd.Parameters.AddWithValue("@TagList", String.Join(",", Tags));
                }
                else
                {
                    cmd.Parameters.AddWithValue("@TagList", "");
                }

                sqlcn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read())
                    {
                        throw new ApplicationException("Error saving LogBookEntry");
                    }
                    Id = SqlHelper.Read<int>(dr, "Id");
                }
                sqlcn.Close();
            }
        }

        private void ProcessCustomTags()
        {
            char[] enders = new char[] {' ',',','#'};
            while (this.Text.StartsWith("#"))
            {
                int hashPos = 1;
                while (this.Text[hashPos] == '#')
                {
                    hashPos++;
                }
                int end = this.Text.IndexOfAny(enders, hashPos);
                if (end != -1)
                {
                    string tag = this.Text.Substring(hashPos - 1, (end - hashPos) + 1);
                    while (end < this.Text.Length && (this.Text[end] == ',' || this.Text[end] == ' '))
                    {
                        end++;
                    }
                    this.Text = this.Text.Substring(end);

                    if (this.Tags == null)
                    {
                        this.Tags = new List<string>();
                    }
                    if (!this.Tags.Contains(tag))
                    {
                        this.Tags.Add(tag);
                    }
                }
            }
        }
    }

    public sealed class LogBookTagRepository
    {
        private static readonly Lazy<LogBookTagRepository> lazy = new Lazy<LogBookTagRepository>(() => new LogBookTagRepository());
        public static LogBookTagRepository Repo { get { return lazy.Value; }}

        internal class CachedRepo
        {
            internal DateTime Timestamp;
            internal List<ILogTag> Tags;
        }

        private const int CacheLifetimeMinutes = 30;
        private const string CacheKey = "FzCommon.LogBookTagRepository";
        
        private LogBookTagRepository()
        {
            m_cache = null;
            m_factories = new Dictionary<string, ILogBookTaggableFactory>();

            m_factories[SensorLocationBase.TagCategory] = new SensorLocationBase.SensorLocationTaggableFactory();
            m_factories[DeviceBase.TagCategory] = new DeviceBase.DeviceTaggableFactory();
            m_factories[ReceiverBase.TagCategory] = new ReceiverBase.ReceiverTaggableFactory();
        }

        public void AddMemoryCache(IMemoryCache cache)
        {
            lock (this)
            {
                if (m_cache == null)
                {
                    m_cache = cache;
                }
            }
        }

        public void FlushCache()
        {
            lock (this)
            {                    
                if (this.m_cache != null)
                {
                    this.m_cache.Remove(CacheKey);
                }
            }
        }

        public async Task<List<ILogTag>> MapTags(List<string> stringTags)
        {
            List<ILogTag> availableTags = await GetAvailableTags();
            List<ILogTag> ret = new List<ILogTag>();

            foreach (string stringTag in stringTags)
            {
                ILogTag tag = availableTags.FirstOrDefault(t => t.GetTag() == stringTag);
                if (tag == null)
                {
                    if (stringTag.StartsWith("#"))
                    {
                        tag = new LogBook.CustomTag(stringTag);
                    }
                    else
                    {
                        //$ TODO: Have a fallback for trying to generate taggables for e.g. deleted locations
                    }
                }
                if (tag != null)
                {
                    ret.Add(tag);
                }
            }
            return ret;
        }

        public bool IsTagListCached()
        {
            lock (this)
            {
                if (this.m_cache == null)
                {
                    return false;
                }
                CachedRepo cachedRepo = this.m_cache.Get<CachedRepo>(CacheKey);
                if (cachedRepo != null && (cachedRepo.Timestamp.AddMinutes(CacheLifetimeMinutes) >= DateTime.UtcNow))
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<List<ILogTag>> GetAvailableTags()
        {
            List<ILogTag> ret = null;
            lock (this)
            {
                if (this.m_cache != null)
                {
                    CachedRepo cachedRepo = this.m_cache.Get<CachedRepo>(CacheKey);
                    if (cachedRepo != null && (cachedRepo.Timestamp.AddMinutes(CacheLifetimeMinutes) >= DateTime.UtcNow))
                    {
                        return cachedRepo.Tags;
                    }
                }
            }

            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                ret = new List<ILogTag>();

                using (SqlCommand cmd = new SqlCommand("GetDistinctLogBookTags", sqlcn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (await dr.ReadAsync())
                        {
                            ret.Add(new LogBook.CustomTag(SqlHelper.Read<string>(dr, "Tag")));
                        }
                    }
                }

                foreach (string category in m_factories.Keys)
                {
                    foreach (ILogTaggable taggable in await m_factories[category].GetAvailableTaggables(sqlcn, category))
                    {
                        ret.Add(new LogBook.AutoTag(category, taggable));
                    }
                }
                sqlcn.Close();
            }

            lock (this)
            {
                if (this.m_cache != null)
                {
                    CachedRepo repo = new CachedRepo()
                    {
                        Timestamp = DateTime.UtcNow,
                        Tags = ret,
                    };
                    this.m_cache.Set<CachedRepo>(CacheKey, repo);
                }
            }
            return ret;
        }

        public async Task<List<ILogTag>> GetAvailableTags(string category)
        {
            List<ILogTag> alltags = await GetAvailableTags();
            List<ILogTag> ret = new List<ILogTag>();
            foreach (ILogTag tag in alltags)
            {
                ILogTaggable taggable = tag.GetTaggable();
                if (taggable != null)
                {
                    if (taggable.GetTagCategory() == category)
                    {
                        ret.Add(tag);
                    }
                }
            }
            return ret;
        }
        
        public void AddFactory(string category, ILogBookTaggableFactory factory)
        {
            lock (this)
            {
                m_factories[category] = factory;
            }
        }

        private IMemoryCache m_cache;
        private Dictionary<string, ILogBookTaggableFactory> m_factories;
    }

    public class LogBook
    {
        public class CustomTag : ILogTag
        {
            public CustomTag(string tag)
            {
                if (!tag.StartsWith("#"))
                {
                    throw new ApplicationException("Custom tags must start with '#'");
                }
                if (tag.Contains(","))
                {
                    throw new ApplicationException("Custom tags cannot contain ','");
                }
                m_tag = tag;
            }
            public string GetTag()
            {
                return m_tag;
            }
            public string GetTagName()
            {
                return m_tag;
            }
            public ILogTaggable GetTaggable()
            {
                return null;
            }
            private string m_tag;
        }

        public class AutoTag : ILogTag
        {
            public AutoTag(string category, ILogTaggable taggable)
            {
                m_category = category;
                m_taggable = taggable;
            }
            public string GetTag()
            {
                return AutoTagPrefix + m_category + "-" + m_taggable.GetTagId();
            }
            public string GetTagName()
            {
                return m_taggable.GetTagName();
            }
            public ILogTaggable GetTaggable()
            {
                return m_taggable;
            }
            private string m_category;
            private ILogTaggable m_taggable;
        }

        public const string AutoTagPrefix = "auto-";

        private static void LogChange(int userId, string email, string objType, string obj, ILogTaggable taggable, string changeDescription, string reason)
        {
            LogBookEntry entry = new LogBookEntry()
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                IsDeleted = false,
                Tags = new List<string>(),
            };
            string taggableName = null;
            string entryDesc = "";
            if (taggable != null)
            {
                taggableName = taggable.GetTagName();
            }
            else
            {
                taggableName = objType + ": " + obj;
                entryDesc = taggableName + " -- ";
            }
            
            if (String.IsNullOrEmpty(reason))
            {
                entry.Text = String.Format("{0}{1} by {2}", entryDesc, changeDescription, email);
            }
            else
            {
                entry.Text = String.Format("{0}{1} by {2}: {3}", entryDesc, changeDescription, email, reason);
            }
            if (taggable != null)
            {
                AutoTag tag = new AutoTag(taggable.GetTagCategory(), taggable);
                entry.Tags.Add(tag.GetTag());
            }
            else
            {
                entry.Tags.Add("#" + objType);
            }

            SlackClient.SendLogChangeNotification(userId, email, taggableName, changeDescription, reason);

            entry.Save();
        }

        public static void LogEdit(int userId, string email, ILogTaggable taggable, string reason)
        {
            LogChange(userId, email, null, null, taggable, "Edited", reason);
        }

        public static void LogEdit(int userId, string email, string objType, string obj, string reason)
        {
            LogChange(userId, email, objType, obj, null, "Edited", reason);
        }

        public static void LogDelete(int userId, string email, ILogTaggable taggable, string reason)
        {
            LogChange(userId, email, null, null, taggable, "Deleted", reason);
        }

        public static void LogDelete(int userId, string email, string objType, string obj, string reason)
        {
            LogChange(userId, email, objType, obj, null, "Deleted", reason);
        }

        public static void LogUndelete(int userId, string email, ILogTaggable taggable, string reason)
        {
            LogChange(userId, email, null, null, taggable, "Undeleted", reason);
        }

        public static void LogUndelete(int userId, string email, string objType, string obj, string reason)
        {
            LogChange(userId, email, objType, obj, null, "Undeleted", reason);
        }

        public static void LogUndeleteObjectList(int userId, string email, string objType, IEnumerable<int> objids, string reason)
        {
            StringBuilder sbObj = new StringBuilder();
            foreach (int id in objids)
            {
                if (sbObj.Length > 0)
                {
                    sbObj.Append(", ");
                }
                sbObj.Append(id.ToString());
            }
            LogChange(userId, email, objType + " list", sbObj.ToString(), null, "Undeleted", reason);
        }

        public static async Task<List<LogBookEntry>> GetEntriesAsync(SqlConnection sqlcn, ILogTaggable thing)
        {
            //$ thing
            List<LogBookEntry> ret = new List<LogBookEntry>();
            Dictionary<int, List<string>> alltags = await GetLogBookEntryTags(sqlcn);
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM LogBookEntries", sqlcn);
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    LogBookEntry entry = InstantiateFromReader(reader);
                    entry.Tags = alltags.ContainsKey(entry.Id) ? alltags[entry.Id] : null;
                    ret.Add(entry);
                }
            }
            return ret;
        }

        //$ TODO: Figure out how this works when filtering by tags in the first place...
        private static async Task<Dictionary<int, List<string>>> GetLogBookEntryTags(SqlConnection sqlcn)
        {
            SqlCommand cmd = new SqlCommand($"SELECT Id,Tag FROM LogBookEntryTags", sqlcn);
            Dictionary<int, List<string>> ret = new Dictionary<int, List<string>>();
            List<String> curTags = null;
            int curEntry = 0;
            try
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int entry = SqlHelper.Read<int>(reader, "Id");
                        if (entry != curEntry)
                        {
                            ret[curEntry] = curTags;
                            curEntry = entry;
                            curTags = new List<string>();
                        }
                        curTags.Add(SqlHelper.Read<string>(reader, "Tag"));
                    }
                }
                if (curEntry != 0 && curTags.Count > 0)
                {
                    ret[curEntry] = curTags;
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "LogBook.GetLogBookEntryTags", ex);
                return null;
            }
            return ret;
        }

        public static async Task MarkEntriesAsDeleted(IEnumerable<int> readingIds)
        {
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                await SqlHelper.CallIdListProcedure(sqlcn, "MarkLogBookEntriesAsDeleted", readingIds, 180);
                sqlcn.Close();
            }
        }

        public static async Task MarkEntriesAsUndeleted(IEnumerable<int> readingIds)
        {

            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                await SqlHelper.CallIdListProcedure(sqlcn, "MarkLogBookEntriesAsUndeleted", readingIds, 180);
                sqlcn.Close();
            }
        }
        private static LogBookEntry InstantiateFromReader(SqlDataReader reader)
        {
            LogBookEntry entry = new LogBookEntry()
            {
                Id = SqlHelper.Read<int>(reader, "Id"),
                UserId = SqlHelper.Read<int>(reader, "UserId"),
                Timestamp = SqlHelper.Read<DateTime>(reader, "Timestamp"),
                Text = SqlHelper.Read<string>(reader, "Text"),
                IsDeleted = SqlHelper.Read<bool>(reader, "IsDeleted"),
            };
            return entry;
        }
        
        private static string GetColumnList()
        {
            return "Id, UserId, Timestamp, Text, IsDeleted";
        }

    }
}
