using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;
using Lastfm.Services;
using Lastfm.Scrobbling;

namespace SocialTags
{
    class Program
    {
        public Session doAuthentication()
        {
            // Get your own API_KEY and API_SECRET from http://www.last.fm/api/account      
            string API_KEY = "df763d78b6d98a624a21e4f46e5af0ec";
            string API_SECRET = "42cb22f3b759e4fb185b87d040c9cde2";
            // Creating an unauthenticated session that could only allow me   
            // to perform read operations.     
            Session session = new Session(API_KEY, API_SECRET);
            string username = "kangism";
            string password = "socsoc";
            string md5password = Lastfm.Utilities.md5(password);
            // Authenticate it with a username and password to be able    
            // to perform write operations and access this user's profile    
            // private data.  
            session.Authenticate(username, md5password);
            return session;
        }

        public TopTag[] getTopTagsForTrack(string artistName, string trackName, Session session)
        {
            Track track = new Track(artistName, trackName, session);
            TopTag[] topTagsForTrack = null;
            try
            {
                topTagsForTrack = track.GetTopTags();
            }
            catch (Exception e)
            {
                if(e.Message.Equals("InvalidParameters: No track found"))
                {
                    topTagsForTrack = null;
                }
            }
            return topTagsForTrack;
        }

        public TopTag[] getTopTagsForArtist(string artistName, Session session)
        {
            Artist artist = new Artist(artistName, session);
            TopTag[] topTagsForArtist = null;
            try
            {
                topTagsForArtist = artist.GetTopTags();
            }
            catch (Exception)
            {
                topTagsForArtist = null;
            }
            return topTagsForArtist;
        }

        public ArrayList computeScore(string[] trackNamesAndArtists, Session session)
        {
            ArrayList trackTagsList = new ArrayList();
            int index = 1;

            foreach (string s in trackNamesAndArtists)
            {
                string[] temp = s.Split('-');
                string trackName = temp[1].Replace("_", " ");
                string artistName = temp[0].Replace("_", " ");
                if (trackName.Contains("dont"))
                {
                    trackName = trackName.Replace("dont", @"don't");
                }
                else if (trackName.Contains("don t"))
                {
                    trackName = trackName.Replace("don t", @"don't");
                }
                else if (trackName.Contains(" ve "))
                {
                    trackName = trackName.Replace(" ve ", @"'ve ");
                }
                System.Console.WriteLine("fetching tags for track: ({0})", trackName);
                System.Console.WriteLine("{0} tracks left", trackNamesAndArtists.Length - index++);
                TopTag[] trackToptags = this.getTopTagsForTrack(artistName, trackName, session);
                TopTag[] artistToptags = this.getTopTagsForArtist(artistName, session);
                object[] obj = new object[5];
                obj[0] = obj[1] = obj[2] = obj[3] = obj[4] = null;
                obj[0] = trackName;
                obj[1] = artistName;
                obj[2] = trackToptags;
                obj[3] = artistToptags;
                trackTagsList.Add(obj);

                //bool exist = false;
                //foreach(object[] o in artistTagsList)
                //{
                //    if (o[0].ToString().Equals(artistName))
                //    {
                //        exist = true;
                //        return;
                //    }
                //}
                //if (exist.Equals(false))
                //{
                //    TopTag[] topTagsForArtist = SocialTags.getTopTagsForArtist(artistName, session);
                //    object[] o = new object[2];
                //    o[0] = artistName;
                //    o[1] = topTagsForArtist;
                //    artistTagsList.Add(o);
                //}
            }
            return trackTagsList;
        }

        public void merge(ArrayList trackTagsList)
        {            
            foreach (object[] tr in trackTagsList)
            {
                SortedList<string, int> trackTagWeightList = new SortedList<string, int>();
                SortedList<string, int> artistTagWeightList = new SortedList<string, int>();
                TopTag[] topTagsForTrack = ((TopTag[])(tr[2]));
                if(topTagsForTrack != null)
                    for (int i = 0; i < topTagsForTrack.Length; i++)
                    {
                        trackTagWeightList.Add(topTagsForTrack[i].Item.Name, topTagsForTrack[i].Weight);
                    }

                TopTag[] topTagsForArtist = ((TopTag[])(tr[3]));
                if (topTagsForArtist != null)
                    for (int i = 0; i < topTagsForArtist.Length; i++)
                    {
                        artistTagWeightList.Add(topTagsForArtist[i].Item.Name, topTagsForArtist[i].Weight);
                    }

                //merge to artistTagWeightList
                for (int i = 0; i < trackTagWeightList.Keys.Count; i++)
                {
                    string tag = trackTagWeightList.Keys[i];
                    if (artistTagWeightList.ContainsKey(tag))
                    {
                        artistTagWeightList[tag] += trackTagWeightList[tag];
                    }
                    else
                        artistTagWeightList.Add(tag, trackTagWeightList[tag]);
                }
                tr[4] = artistTagWeightList;

            }
        }

        public string[] read(string file)
        {
            string[] lines = null;
            try
            {
                lines = File.ReadAllLines(file);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return lines;
        }

        public void print(ArrayList trackTagsList, string file)
        {
            FileStream fs = File.Create(file);
            StreamWriter sw = new StreamWriter(fs);
            foreach (object[] tr in trackTagsList)
            {
                string line;
                string trackName = (string)(tr[0]);
                string artistName = (string)(tr[1]);
                line = artistName + "," + trackName + ",";
                SortedList<string, int> tagWeightList = (SortedList<string, int>)(tr[4]);
                IEnumerator <KeyValuePair<string, int>> enumerator = tagWeightList.GetEnumerator();
                while(enumerator.MoveNext())
                    line += ("[" + enumerator.Current.Key + "," + enumerator.Current.Value + "]");
                sw.WriteLine(line);
            }
            sw.Flush();
            sw.Close();
            fs.Close();
            
        }

        static void Main(string[] args)
        {
            DateTime time1 = DateTime.Now;
            Program SocialTags = new Program();
            Session session = null;
            string[] trackNamesAndArtists = null;
            ArrayList trackTagsList = null;

            //perform authentication
            session = SocialTags.doAuthentication();
            
            //read file
            trackNamesAndArtists = SocialTags.read(@"songNames.txt"); 

            //compute scores for each track and artist
            trackTagsList = SocialTags.computeScore(trackNamesAndArtists, session);

            //merge the track scores and artist scores into one integrated score
            SocialTags.merge(trackTagsList);

            //write result to file
            SocialTags.print(trackTagsList, @"ScoreFile.txt");

            DateTime time2 = DateTime.Now;
            TimeSpan span = new TimeSpan(time2.Ticks - time1.Ticks);
            Console.WriteLine(span);
            Console.ReadLine();

            
            //foreach (TopTag t in topTagsForTrack)
            //    Console.WriteLine(t.Item + " " + t.Weight);

            //Console.WriteLine("-----------");
            //Artist artist = new Artist(artistName, session);
            //TopTag[] topTagsForArtist = artist.GetTopTags(10);
            //foreach (TopTag t in topTagsForArtist)
            //    Console.WriteLine(t.Item + " " + t.Weight);


            //// You can now use the "session" object with everything in your project.
            //Artist artist = new Artist("system of a down", session);
            //// Tag it.   
            //artist.AddTags("classical", "smooth");
            //// Display your current tags for system of a down.   
            //foreach (Tag tag in artist.GetTags())
            //    Console.WriteLine(tag);
            //// Remove tags from it    
            //artist.RemoveTags("classical", "smooth");

            //Album album = new Album("westlife", "Coast to Coast", session);
            //Track[] tracks = album.GetTracks();
            //Console.WriteLine(tracks.Length);
            //Console.WriteLine(tracks[0]);
            //Console.WriteLine(tracks[0].GetAlbum());
            //Console.WriteLine(tracks[0].GetDuration());
            //Tag[] tagss = tracks[0].GetTags();



            //// Get your own API_KEY and API_SECRET from http://www.last.fm/api/account    
            //string API_KEY = "df763d78b6d98a624a21e4f46e5af0ec";
            //string API_SECRET = "42cb22f3b759e4fb185b87d040c9cde2";    
            //// Creating an unauthenticated session that could only allow me   
            //// to perform read operations.     
            //Session session = new Session(API_KEY, API_SECRET);     
            //// Generate a web authentication url     
            //string url = session.GetWebAuthenticationURL();    
            //// Ask the user to open it and allow you to access his/her profile.  
            //Console.WriteLine("Please open the following url in your web browser and follow the procedure, then press Enter...");  
            //Console.WriteLine(url);        
            //// Wait for it...   
            //Console.ReadLine();          
            //// Now that he's pressed Enter    
            //session.AuthenticateViaWeb();
            //// You can now use the authenticated "session" object with everything in your project.
            //Track t = new Track("westlife", "my love", session);
            //Tag[] tags = t.GetTags();
            //Console.WriteLine(tags.ToString());

            //Artist artist = new Artist("system of a down", session);
            //Console.WriteLine("111");
            //foreach (Tag tag in artist.GetTags())
            //    Console.WriteLine(tag);
            //// Tag it.   
            //artist.AddTags("classical", "smooth");
            //// Display your current tags for system of a down.   
            //Console.WriteLine("222");
            //foreach (Tag tag in artist.GetTags())
            //    Console.WriteLine(tag);
            //// Remove tags from it    
            //artist.RemoveTags("classical", "smooth");
            //Console.WriteLine("333");
            //foreach (Tag tag in artist.GetTags())
            //    Console.WriteLine(tag);

            //Album album = new Album("westlife", "Coast to Coast", session);
            //Track[] tracks = album.GetTracks();
            //Console.WriteLine(tracks.Length);
            //Console.WriteLine(tracks[0]);
            //Console.WriteLine(tracks[0].GetAlbum());
            //Console.WriteLine(tracks[0].GetDuration());

            //Tag[] tagss = tracks[0].GetTags();

            


            //Console.WriteLine("tst");
            //Session ses = new Session("df763d78b6d98a624a21e4f46e5af0ec", "42cb22f3b759e4fb185b87d040c9cde2");
            //ses.Authenticate("kangism", "0407150022");
            
            //Track t = new Track("","",ses);
        }
    }
}
