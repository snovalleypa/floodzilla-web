using Microsoft.Extensions.Caching.Memory;

using FloodzillaWeb.Cache;
using FzCommon;

namespace FloodzillaWeb.Models
{
    public class LogBookModel
    {
        public static void EnsureTagCache(IMemoryCache memoryCache)
        {
            // It's ok to do this multiple times. 
            LogBookTagRepository.Repo.AddMemoryCache(memoryCache);

            if (!LogBookTagRepository.Repo.IsTagListCached())
            {
                LogBookTagRepository.Repo.GetAvailableTags();
            }
        }
      
        public class LogBookEntry
        {
            public int Id;
            public int UserId;
            public string Email;
            public DateTime Timestamp;
            public string Text;
            public bool IsDeleted;
            public List<LogBookModel.LogBookTag> Tags;

            public async Task InitializeLogBookEntry(ApplicationCache cache, FzCommon.LogBookEntry source)
            {
                this.Id = source.Id;
                this.UserId = source.UserId;
                this.Timestamp = source.Timestamp;
                this.Text = source.Text;
                this.IsDeleted = source.IsDeleted;

                var user = cache.GetUsers().SingleOrDefault(e => e.Id == UserId);
                if (user == null)
                {
                    this.Email = "<unknown user>";
                }
                else
                {
                    this.Email = user.AspNetUser.Email;
                }
                
                List<ILogTag> iTags = await LogBookTagRepository.Repo.MapTags(source.Tags);
                this.Tags = new List<LogBookModel.LogBookTag>();
                foreach (ILogTag iTag in iTags)
                {
                    LogBookModel.LogBookTag tag = new LogBookModel.LogBookTag()
                    {
                        Tag = iTag.GetTag(),
                        Name = iTag.GetTagName(),
                    };
                    if (iTag.GetTaggable() != null)
                    {
                        tag.Category = iTag.GetTaggable().GetTagCategory();
                    }
                    this.Tags.Add(tag);
                }
            }
        }

        public class LogBookTag
        {
            public string Tag;
            public string Name;
            public string Category;
        }
    }
}
