using YoutubeExplode;
using YoutubeExplode.Common; // Thumbnail ve Video ID özellikleri için gerekli
using YoutubeExplode.Search;
using System.Linq;

namespace Learnicted.Services
{
    public class YouTubeService
    {
        private readonly YoutubeClient _youtube;

        public YouTubeService()
        {
            _youtube = new YoutubeClient();
        }

        public async Task<List<VideoModel>> GetRelatedVideos(string subject)
        {
            var videos = new List<VideoModel>();

            // CollectAsync yerine GetVideosAsync sonucunu alıyoruz
            var searchResults = await _youtube.Search.GetVideosAsync($"{subject} konu anlatımı");

            // Take(6) kullanarak en alakalı ilk 6 videoyu güvenli bir şekilde listeye ekliyoruz
            foreach (var video in searchResults.Take(6))
            {
                videos.Add(new VideoModel
                {
                    Title = video.Title,
                    VideoId = video.Id.Value,
                    // Çözünürlüğü en yüksek thumbnail'ı seçiyoruz
                    Thumbnail = video.Thumbnails.OrderByDescending(t => t.Resolution.Width).FirstOrDefault()?.Url,
                    ChannelTitle = video.Author.Title
                });
            }

            return videos;
        }
    }

    public class VideoModel
    {
        public string Title { get; set; }
        public string VideoId { get; set; }
        public string Thumbnail { get; set; }
        public string ChannelTitle { get; set; }
    }
}