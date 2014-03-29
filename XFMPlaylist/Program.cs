using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using System.Net.Http;
using System.IO;

namespace XFMPlaylist
{
    class Program
    {
        static void Main(string[] args)
        {

            var shows = TrackLister.GetShows();

            var sb = new StringBuilder();

            foreach (var show in shows)
            {
                foreach (var track in show.Tracks)
                {
                    sb.AppendLine(string.Format("{2},{3}",
                        show.Series,
                        show.Date,
                        track.Artist,
                        track.Name));
                }
            }

            File.WriteAllText(@"xfmtracks.txt", sb.ToString());
        }
    }


    public class TrackLister
    {
        public TrackLister()
        {
        }

        public static List<Show>  GetShows()
        {
            var output = new List<Show>();

            var headerID = "List_of_Episodes";

            var baseurl = "http://www.pilkipedia.co.uk";

            var url = "http://www.pilkipedia.co.uk/wiki/index.php?title=Xfm_Series_2";

            using (var client = new HttpClient())
            {
                var htmlString = client.GetStringAsync(url).Result;

                var html = new HtmlDocument();
                html.LoadHtml(htmlString);
                var document = html.DocumentNode;

                var selector = string.Format("#{0}", headerID);

                var headerSpan = document.QuerySelector(selector);

                var table = headerSpan.ParentNode.NextSibling.NextSibling;

                var shows = table
                    .QuerySelectorAll("td + td a")
                    .Where(o => string.Compare(o.InnerHtml, "Download Page", true) != 0)
                    .Select(o => new Show
                    {
                        Series = "2",
                        Date = o.InnerHtml,
                        Url = string.Format("{0}{1}", baseurl, o.Attributes["href"].Value)
                    })
                    .ToList();

                foreach (var show in shows)
                {
                    try
                    {
                        GetTracks(show.Url, show);
                    }
                    catch { }
                }
                return shows;
            }


        }

        private static void GetTracks(string showUrl, Show show)
        {
            using (var client = new HttpClient())
            {
                var htmlString = client.GetStringAsync(showUrl).Result;
                var html = new HtmlDocument();
                html.LoadHtml(htmlString);
                var document = html.DocumentNode;

                var headerSpan = document.QuerySelector("#Playlist");

                var trackListString = headerSpan.ParentNode.NextSibling.NextSibling.InnerText;

                var tracklisting = trackListString.Split("\n".ToCharArray()).Where(o => !string.IsNullOrWhiteSpace(o)).ToList();

                foreach (var track in tracklisting)
                {
                    var parts = track.Split("-".ToCharArray());
                    show.Tracks.Add(new Track
                    {
                        Artist = parts.First().Trim(),
                        Name = parts.Last().Trim()
                    });
                }

            }
        }

    }

    public class Show
    {
        public Show ()
        {
            this.Tracks = new List<Track>();
        }

        public string Series { get; set; }
        public string Date { get; set; }
        public string Url { get; set; }

        public List<Track> Tracks { get; set; }
    }

    public class Track
    {
        public string Artist { get; set; }
        public string Name { get; set; }
    }
}
