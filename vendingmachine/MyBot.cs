using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Audio;
using System.IO;
using System.Media;
using System.Threading;
using System.Net;
using System.Web;
using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Collections;
using System.Diagnostics;
using System.Xml;
using System.ServiceModel;
using System.ServiceModel.Syndication;

namespace vendingmachine
{
    class MyBot
    {
        //Setups we need for commands later
        DiscordClient discord;
        CommandService commands;
        AudioService audio;
        System.Collections.ArrayList musicQ;
        int _nextClientId;

        //Names of files we can send off to a chat room when given a command
        //Not elegant, needs replacing
        string[] swfs;
        string[] topics;
        string[] album;

        //Misc setups
        Boolean skipB;
        Boolean addMp3;
        Random rand;
        public MyBot()
        {
            discord = new DiscordClient(x =>
            {
                x.LogLevel = LogSeverity.Info;   //Error logging
                x.LogHandler = Log;             //Error logging
            });

            discord.UsingCommands(x =>  //We want to be able to use commands
            {
                x.PrefixChar = '-';//What prefix is used for the bot to recognize a command
                x.AllowMentionPrefix = true;//Also allows users to mention bot for a command
            });

            discord.UsingAudio(x =>  //We want to use audio, but not listen
            {
                x.Mode = AudioMode.Outgoing;
            });
            
            commands = discord.GetService<CommandService>();
            audio = discord.GetService<AudioService>();


            //setups for files to be sent. This really needs a re-work when I have time
            addMp3 = false;
            swfs = new string[] {
                "enterthefilenameshere.swf"
            };

            //Used for setting up random weekly topics to talk about in the server I helped manage
            //Definitely needs a change when I get around to it. Possibly reading from a file or something similar.
            topics = new string[] {
                "topic 1",
                "topic 2"
            };

            //Not sure if this is the way to do this just yet. 
            //Commands need to be set up in this way in the current implementation.
            musicQ = new System.Collections.ArrayList();
            skipB = false;
            rand = new Random();
            JoinRequest();
            news();
            help();
            //PlayRequest();
            SendSwf();
            sendQ();
            skip();
            PickOne();
            playAlbum();

            discord.ExecuteAndWait(async () =>
            {
                await discord.Connect("Thisismysecretkey,you'renotallowedtoknowthis", TokenType.Bot);//Used for login information. Could also use email/pass
                discord.SetGame("Ready and waiting!"); //Set our initial "Playing X" message
            });
        }

        private void SendSwf()  //Sends a random swf from our folder, only works if under 8mb (thanks discord)
        {
            commands.CreateCommand("swf").Do(async (e) =>
            {
                Channel voiceChannel = e.User.VoiceChannel;
                string swf2Post = swfs[rand.Next(swfs.Length)];
                string swfName = swf2Post.Remove(0, 4);
                swfName = swfName.Remove(swfName.Length - 4, 4);
                await e.Channel.SendMessage("Sending: " + swfName +" if file is under 8mb");
                await e.Channel.SendFile(swf2Post);
            });
        }

        private void PickOne()  //Picks a topic from our list of topics
        {
            commands.CreateCommand("mystery").Do(async (e) =>
            {
                string topic = topics[rand.Next(topics.Length)];
                await e.Channel.SendMessage("DRUM ROLL PLEASE! :drum:");
                System.Threading.Thread.Sleep(5000); //Pause for dramatic effect
                await e.Channel.SendMessage(":confetti_ball: " + topic + "! :confetti_ball: ");
                //await e.Channel.Edit("Mystery Box", topic);
            });
        }

        private void help()  //Sends our handy dandy google doc
        {
            commands.CreateCommand("help").Do(async (e) =>
            {
                await e.Channel.SendMessage("Check it out!: comingsoon(tm)");
            });
        }

        private void playAlbum()
        {
            commands.CreateCommand("pa").Parameter("name", ParameterType.Unparsed).Do(async (e) =>
            {
                await e.Channel.SendMessage("Adding album to queue: " + e.GetArg("name")); //Add the song to play to our list of songs
                var server = e.Channel.Server;
                skipB = false;
                Channel voiceChannel = e.User.VoiceChannel; //Find which voice channel the person who sent this command is in
                var _vClient = await discord.GetService<AudioService>() // We use GetService to find the AudioService that we installed earlier. In previous versions, this was equivelent to _client.Audio()
                 .Join(voiceChannel);
                string filePath = "mu/"; 
                filePath = filePath.Insert(3, e.GetArg("name"));
                album = Directory.GetFiles(filePath);
                int count = 0;
                addMp3 = false;
                while(count<album.Length) //While we still have songs in the album to add...
                {
                    album[count] = album[count].Remove(album[count].Length-4,4);
                    album[count] = album[count].Replace(@"\","/");
                    album[count] = album[count].Insert(album[count].Length,".mp3");
                    musicQ.Add(album[count]);
                    count++;
                }
                if (musicQ.Count == album.Length)
                {
                    PlayRequest(musicQ, _vClient); //Uses our "play single song" method
                }
            });
        }

        private void JoinRequest()
        {
            commands.CreateCommand("p").Parameter("name",ParameterType.Unparsed).Do(async (e) =>
            {
                await e.Channel.SendMessage("Adding song to queue: "+e.GetArg("name"));
                var server = e.Channel.Server;
                skipB = false;
                Channel voiceChannel = e.User.VoiceChannel;
                var _vClient = await discord.GetService<AudioService>() // We use GetService to find the AudioService that we installed earlier. In previous versions, this was equivelent to _client.Audio()
                 .Join(voiceChannel);
                //Should probably use a better method to search for songs, but this is an easy add and doesn't take long. Could cause issues with similar song titles though.
                foreach(string s in Directory.GetFiles("mu/", "*.*", SearchOption.AllDirectories))
                {
                    if (s.Contains(e.GetArg("name")))
                    {
                        musicQ.Add(s);
                    }
                }
                if (musicQ.Count==1) {
                    PlayRequest(musicQ, _vClient);
                }
                
            });
        }

        private void PlayRequest(ArrayList musicQ, IAudioClient audio)  //FOR PLAYING OUR OWN LOCAL MP3 FILES
        {
            while (musicQ.Count!=0) {
                string filePath = (string)musicQ[0];
                if (addMp3==true) {
                    filePath = filePath.Insert(0, "mu/");
                    filePath = filePath.Insert(filePath.Length, ".mp3");
                }
                System.Console.Write(filePath);
                string songName = filePath.Remove(0, 3);
                songName = songName.Remove(songName.Length - 4, 4);
                discord.SetGame(songName);
                var channelCount = discord.GetService<AudioService>().Config.Channels; // Get the number of AudioChannels our AudioService has been configured to use.
                var OutFormat = new WaveFormat(48000, 16, channelCount); // Create a new Output Format, using the spec that Discord will accept, and with the number of channels that our client supports.
                using (var MP3Reader = new Mp3FileReader(filePath)) // Create a new Disposable MP3FileReader, to read audio from the filePath parameter
                using (var resampler = new MediaFoundationResampler(MP3Reader, OutFormat)) // Create a Disposable Resampler, which will convert the read MP3 data to PCM, using our Output Format
                {
                    resampler.ResamplerQuality = 60; // Set the quality of the resampler to 60, the highest quality
                    int blockSize = OutFormat.AverageBytesPerSecond / 50; // Establish the size of our AudioBuffer
                    byte[] buffer = new byte[blockSize];
                    int byteCount;

                    while (((byteCount = resampler.Read(buffer, 0, blockSize)) > 0) && skipB!=true) // Read audio into our buffer, and keep a loop open while data is present
                    {
                        if (byteCount < blockSize)
                        {
                            // Incomplete Frame
                            for (int i = byteCount; i < blockSize; i++)
                                buffer[i] = 0;
                        }

                        audio.Send(buffer, 0, blockSize); // Send the buffer to Discord
                    }
                    skipB = false;
                }
                musicQ.RemoveAt(0);//song over so remove from queue
            }
        }

        //Wanted some practice with using news feeds. requires the command, and a country tag. e.g. "-n nz" gives New Zealand news.
        private void news()
        {
            commands.CreateCommand("n").Parameter("country", ParameterType.Unparsed).Do((e) =>
            {
                //string url = "http://news.google.com/news?cf=all&hl=en&pz=1&ned=nz&output=rss";
                string url = "http://news.google.com/news?cf=all&hl=en&pz=1&ned=" +e.GetArg("country")+ "&output=rss";
                XmlReader reader = XmlReader.Create(url);
                SyndicationFeed feed = SyndicationFeed.Load(reader);
                reader.Close();
                String subject = "";
                foreach (SyndicationItem item in feed.Items)
                {
                    subject=subject+item.Title.Text+"\n";
                }
                e.Channel.SendMessage(subject);
            });
        }

        //More feed practice. Gives news for a particular platform.
        private void vidya()
        {
            commands.CreateCommand("v").Parameter("platform", ParameterType.Unparsed).Do((e) =>
            {
                //string url = "http://feeds.ign.com/ign/pc-all";
                string url = "http://feeds.ign.com/ign/" + e.GetArg("platform") + "-all";
                XmlReader reader = XmlReader.Create(url);
                SyndicationFeed feed = SyndicationFeed.Load(reader);
                reader.Close();
                String subject = "";
                foreach (SyndicationItem item in feed.Items)
                {
                    subject = subject + item.Title.Text + "\n";
                }
                e.Channel.SendMessage(subject);
            });
        }


        private void sendQ()  //Sends a message containing the current songs in the queue
        {
            commands.CreateCommand("q").Do(async (e) =>
            {
                string send = "";
                int count = 0;
                await e.Channel.SendMessage("1");
                foreach (string s in musicQ)
                {
                    send = send + count;
                    send = send + ": ";
                    string name = s.Split('\\')[1];
                    send = send + name.Remove(s.Length-4,4);
                    send = send + ", ";
                    await e.Channel.SendMessage(name);
                    count = count + 1;
                }
                await e.Channel.SendMessage("Items in the queue: " + send);
            });

        }

        private void skip() //skips the first song (currently playing) in the queue
        {
            commands.CreateCommand("s").Do(async (e) =>
            {
                await e.Channel.SendMessage("Skipping current song!");
                skipB = true;
                discord.SetGame((string)musicQ[0]);
            });

        }

        private void Log(object sender, LogMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
