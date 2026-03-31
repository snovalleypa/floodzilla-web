using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

using FzCommon;
using FzCommon.Map;

namespace FloodzillaWeb.Controllers
{
    [AllowAnonymous]
    public class MapController : Controller
    {
        //$ TODO: Consider locking down our map tile server so that only our app can use it...
        private bool CheckAccess(MapMetadata metadata, string referer, string userAgent)
        {
            return true;
        }

        [Route("maps/{regionId}/{item}")]
        // This handles requests for all of the basic metadata about maps.
        public async Task<IActionResult> GetWebStyles(int regionId, string item)
        {
            MapMetadata metadata = await MapMetadata.Get(regionId);
            if (!CheckAccess(metadata, HttpContext.Request.Headers["Referer"], HttpContext.Request.Headers["User-Agent"]))
            {
                return new ForbidResult();
            }
            string? content = null;
            string? hash = null;
            switch (item)
            {
                case "webstyles":
                    content = metadata.WebStyles;
                    hash = "W/\"" + metadata.WebStylesHash + "\"";
                    break;
                case "mobilestyles":
                    content = metadata.MobileStyles;
                    hash = "W/\"" + metadata.MobileStylesHash + "\"";
                    break;
                case "tiles.json":
                    content = metadata.TileDescJson;
                    hash = "W/\"" + metadata.TileDescJsonHash + "\"";
                    break;
            }
            if (content == null)
            {
                return new NotFoundResult();
            }
            if (HttpContext.Request.Headers.ContainsKey("If-None-Match"))
            {
                string etag = HttpContext.Request.Headers["If-None-Match"];
                if (etag == hash)
                {
                    return new StatusCodeResult(304);
                }
            }
            HttpContext.Response.Headers["Etag"] = hash;
            return new ContentResult()
            {
                ContentType = "application/json",
                Content = content,
            };
        }

        [Route("maps/{regionId}/tiles/{z}/{x}/{y}.pbf")]
        public async Task<IActionResult> GetTile(int regionId, int z, int x, int y)
        {
            // The way that the map system asks for tiles is flipped vertically, so adjust the row
            int gridRow = (1 << z) - 1 - y;
            string expectedHash = HttpContext.Request.Headers["If-None-Match"];
            string? cachedHash = MapCache.Get().GetCachedTileHash(regionId, z, gridRow, x);
            if (expectedHash != null && cachedHash != null && expectedHash == "W/\"" + cachedHash + "\"")
            {
                return new StatusCodeResult(304);
            }
            using (SqlConnection sqlcn = new(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                (byte[]? gzippedData, string? hash) = await MapCache.Get().GetTileForGrid(sqlcn, regionId, z, gridRow, x);
                if (gzippedData == null)
                {
                    return new NotFoundResult();
                }
                FileContentResult result = new(gzippedData, "application/x-protobuf");
                HttpContext.Response.Headers.ContentEncoding = "gzip";
                HttpContext.Response.Headers["Etag"] = "W/\"" + hash + "\"";
                return result;
            }
        }
    }
}

