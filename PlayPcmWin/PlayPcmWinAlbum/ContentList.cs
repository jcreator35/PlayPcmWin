using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace PlayPcmWinAlbum {
    public class ContentList {
        public ContentList() {
            SaveFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar + "PlayPcmWinAlbum";
        }

        /// <summary>
        /// 音声ファイル。
        /// </summary>
        public class AudioFile {
            public string Path { get; set; }
            public string Title { get; set; }
            public int NumOfTracks { get; set; }
            public string AlbumName { get; set; }
            public string ArtistName { get; set; }
            public byte[] AlbumCoverArt { get; set; }
            public PcmDataLib.PcmData Pcm { get; set; }

            public AudioFile(string path, string title, int numOfTracks, string albumName, string artistName,
                    byte[] albumCoverArt, int nChannels, int bitsPerSample,
                    int sampleRate, long numFrames) {
                Path = path;
                Title = title;
                NumOfTracks = numOfTracks;
                AlbumName = albumName;
                ArtistName = artistName;
                AlbumCoverArt = albumCoverArt;

                var pcmData = new PcmDataLib.PcmData();
                pcmData.SetFormat(nChannels, bitsPerSample, bitsPerSample,
                        sampleRate, PcmDataLib.PcmData.ValueRepresentationType.SInt, numFrames);
                pcmData.DisplayName = title;
                pcmData.AlbumTitle = albumName;
                pcmData.ArtistName = artistName; 
                if (0 < albumCoverArt.Length) {
                    pcmData.SetPicture(albumCoverArt.Length, albumCoverArt);
                }
                Pcm = pcmData;
            }
        }

        /// <summary>
        /// アルバム。
        /// </summary>
        public class Album {
            public string Name { get; set; }
            private Dictionary<string, AudioFile> mAudioFileList = new Dictionary<string, AudioFile>();
            public int AudioFileCount { get { return mAudioFileList.Count; } }
            public AudioFile AudioFileNth(int nth) { return mAudioFileList.ElementAt(nth).Value; }
            public void Add(AudioFile af) { mAudioFileList.Add(af.Path, af); }

            public Album(string name) {
                Name = name;
            }
        }

        private List<Album> mAlbumList = new List<Album>();
        private List<AudioFile> mAudioFileList = new List<AudioFile>();
        private Dictionary<string, Album> mAlbumNameToAlbum = new Dictionary<string, Album>();

        public int AlbumCount { get { return mAlbumList.Count; } }
        public Album AlbumNth(int nth) { return mAlbumList[nth]; }

        public void Clear() {
            mAlbumList = new List<Album>();
            mAudioFileList = new List<AudioFile>();
            mAlbumNameToAlbum = new Dictionary<string, Album>();
        }
        
        // 音声ファイルを追加する。
        public void Add(string path, string title, int numOfTracks, string albumName, string artistName, byte[] albumCoverArt,
                int nChannels, int bitsPerSample, int sampleRate, long numFrames) {
            System.Diagnostics.Debug.Assert(albumCoverArt != null);

            var af = new AudioFile(path, title, numOfTracks, albumName, artistName, albumCoverArt, nChannels, bitsPerSample, sampleRate, numFrames);
            mAudioFileList.Add(af);

            Album album = null;
            if (!mAlbumNameToAlbum.ContainsKey(albumName)) {
                // アルバム名が一覧にないときアルバムを作る。
                album = new Album(albumName);

                mAlbumList.Add(album);
                mAlbumNameToAlbum.Add(albumName, album);
            } else {
                album = mAlbumNameToAlbum[albumName];
            }

            album.Add(af);
        }

        public string SaveFolder { get; set; }

        private ReaderWriterLock mLock = new ReaderWriterLock();

        //                                           version=2 "PPWA"
        private static readonly long FILE_VERSION = 0x0000000241575050L;
        private static readonly string MUSIC_LIST_FILE_NAME = Path.DirectorySeparatorChar + "PPWA_MusicList.bin";
        private string mMusicListPath;
        private Dictionary<string, long> mIndex = new Dictionary<string, long>();

        private void SaveString(BinaryWriter bw, string s) {
            var b = Encoding.UTF8.GetBytes(s);
            bw.Write(b.Length);
            bw.Write(b);
        }

        private string LoadString(BinaryReader br) {
            int count = br.ReadInt32();
            var b = br.ReadBytes(count);
            return Encoding.UTF8.GetString(b, 0, count);
        }

        public void Save() {
            mLock.AcquireWriterLock(Timeout.Infinite);

            try {
                if (!Directory.Exists(SaveFolder)) {
                    Directory.CreateDirectory(SaveFolder);
                }
                using (var musicFs = new FileStream(mMusicListPath, FileMode.Create, FileAccess.Write)) {
                    using (var bw = new BinaryWriter(musicFs)) {
                        bw.Write(FILE_VERSION);
                        bw.Write(mAudioFileList.Count);

                        foreach (var item in mAudioFileList) {
                            // string path
                            SaveString(bw, item.Path);

                            {
                                var pcm = item.Pcm;
                                bw.Write(pcm.NumChannels);
                                bw.Write(pcm.BitsPerSample);
                                bw.Write(pcm.SampleRate);
                                bw.Write(pcm.NumFrames);
                            }

                            // string title
                            SaveString(bw, item.Title);
                            // int numOfTracks
                            bw.Write(item.NumOfTracks);
                            // string albumName
                            SaveString(bw, item.AlbumName);
                            // string artistName
                            SaveString(bw, item.ArtistName);
                            // int albumCoverArtSize
                            bw.Write(item.AlbumCoverArt.Length);
                            if (0 < item.AlbumCoverArt.Length) {
                                // int albumCoverArtOffset
                                bw.Write(item.AlbumCoverArt);
                            }
                        }
                    }
                }
            } catch (IOException ex) {
                Console.WriteLine(ex);
            } catch (System.ArgumentException ex) {
                Console.WriteLine(ex);
            }

            mLock.ReleaseWriterLock();
        }

        public bool Load() {
            bool result = true;

            Clear();

            mLock.AcquireWriterLock(Timeout.Infinite);

            mMusicListPath = SaveFolder + MUSIC_LIST_FILE_NAME;

            try {

                using (FileStream indexFs = new FileStream(mMusicListPath, FileMode.Open, FileAccess.Read)) {
                    using (var br = new BinaryReader(indexFs)) {
                        if (br.ReadInt64() != FILE_VERSION) {
                            throw new IOException();
                        }
                        int count = br.ReadInt32();

                        for (int i = 0; i < count; ++i) {
                            // string path
                            string path = LoadString(br);

                            int numChannels = br.ReadInt32();
                            int bitsPerSample = br.ReadInt32();
                            int sampleRate = br.ReadInt32();
                            long numFrames = br.ReadInt64();

                            // string title
                            string title = LoadString(br);
                            // int numOfTracks
                            int numOfTracks = br.ReadInt32();
                            // string albumName
                            string albumName = LoadString(br);
                            // string artistName
                            string artistName = LoadString(br);
                            // int albumCoverArtSize
                            int albumCoverArtLength = br.ReadInt32();
                            var albumCoverArt = new byte[0];
                            if (0 < albumCoverArtLength) {
                                albumCoverArt = br.ReadBytes(albumCoverArtLength);
                            }
                            Add(path, title, numOfTracks, albumName, artistName, albumCoverArt,
                                    numChannels,bitsPerSample,sampleRate,numFrames);
                        }
                    }
                }
            } catch (IOException ex) {
                Console.WriteLine(ex);
                result = false;
            } catch (System.ArgumentException ex) {
                Console.WriteLine(ex);
                result = false;
            }
            mLock.ReleaseWriterLock();
            return result;
        }
    }
}
