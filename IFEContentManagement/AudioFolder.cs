﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace IFEContentManagement
{
    class AudioFolder
    {
        public List<MusicPlaylist> library;
        
        string title;
        string location;

        public AudioFolder() {  }
        
        public AudioFolder(string _location, string _title)
        {
            library = new List<MusicPlaylist>();
            title = _title;
            location = _location;
            if (DiskIO.IsFileExist(ContentLocation, "index.en.json"))
            {
                // go back
                //DiskIO.DeserializeObjectFromJSONFile(this, ContentLocation, "index.en.json");
            }
        }

        private string ContentLocation
        {
            get { return location + "\\" + title; }
        }

        public void CreateNewAudioDirectory()
        {           
            DiskIO.CreateDirectory(location,title);
            DiskIO.DeleteFile(ContentLocation, "index.en.json");
        }

        internal void SavePlaylistLibrary()
        {
            // save all playlists json file
            DiskIO.SaveAsJSONFile(this, this.ContentLocation , "index.en.json");
            // create folder for each playlist and save their music tracks
            foreach(MusicPlaylist pls in this.library)
            {
                string playlistFolder = this.ContentLocation+"\\"+pls.id;
                DiskIO.CreateDirectory(playlistFolder);
                pls.SaveMusicFilesInfos(playlistFolder, "index.Json");
            }
        }
        internal void SavePlaylistLibrary(string _fileName)
        {
            DiskIO.SaveAsJSONFile(this, this.ContentLocation, _fileName);
        }
        private void SavePlaylistLibraryAtLocation(string _fullPath, string _fileName)
        {
            // save all playlists json file
            DiskIO.SaveAsJSONFile(this, _fullPath, _fileName);
            // create folder for each playlist and save their music tracks
            foreach (MusicPlaylist pls in this.library)
            {
                string playlistFolder = _fullPath + "\\" + pls.id;
                //DiskIO.CreateDirectory(playlistFolder);
                pls.SaveMusicFilesInfos(playlistFolder, "index.Json");
            }
        }
        
        internal static AudioFolder SerializeFromJSON(string _audioLocation,string _audioFolderName, string _fileName)
        {
            AudioFolder retVal = new AudioFolder(_audioLocation, _audioFolderName);
            if (DiskIO.IsFileExist(_audioLocation + "\\" + _audioFolderName, _fileName))
            {
                retVal = DiskIO.DeserializeAudioFolderFromFile(_audioLocation + "\\" + _audioFolderName, _fileName);                
                foreach(MusicPlaylist pls in retVal.library)
                {
                    pls.LoadMusicFileInfos(_audioLocation + "\\" + _audioFolderName+"\\"+pls.id,"index.json");
                }
            }
            retVal.SetLocationTitle(_audioLocation, _audioFolderName);
            return retVal;
        }

        private void SetLocationTitle(string _audioLocation, string _audioFolderName)
        {
            this.title = _audioFolderName;
            this.location = _audioLocation;
        }

        internal void SavePlaylistNonEnglishLibrary(Dictionary<string, MusicPlaylist> _dictionary)
        {
            AudioFolder temp = new AudioFolder(this.location, this.title);
            foreach (var item in Enum.GetValues(typeof(Languages)))
            {
                if (_dictionary.ContainsKey(item.ToString()))
                {
                    string header = item.ToString().Substring(0, 2);
                    string fileName = "index." + header + ".json";
                    if (DiskIO.IsFileExist(ContentLocation, fileName))
                    {
                        temp = DiskIO.DeserializeAudioFolderFromFile(ContentLocation, fileName);
                        temp.SetLocationTitle(this.location, this.title);
                        if (temp.HasPlaylistWithID(_dictionary[item.ToString()].id))
                        {
                            temp.RemovePlaylistWithID(_dictionary[item.ToString()].id);
                        }
                        temp.library.Add(_dictionary[item.ToString()]);
                        temp.SavePlaylistLibrary(fileName);
                    }
                    else
                    {
                        temp.library = new List<MusicPlaylist>();
                        temp.library.Add(_dictionary[item.ToString()]);
                        temp.SavePlaylistLibrary(fileName);
                    }
                }
            }
        }

        private void RemovePlaylistWithID(int _id)
        {
            foreach (MusicPlaylist p in library)
                if (p.id == _id)
                {
                    library.Remove(p);
                    return;
                }
            return;
        }

        public bool HasPlaylistWithID(int _id)
        {
            foreach(MusicPlaylist p in library)
            {
                if (p.id == _id) return true;
            }
            return false;
        }

        internal FileCopier[] ExportFilesTo(string _contentLoc, string[] _languages)
        {            
            string newWorkArea = _contentLoc + "\\" + title;
            List<FileCopier> allFilesToCopy = new List<FileCopier>();

            // create root folder
            DiskIO.CreateDirectory(newWorkArea);
            // create each playlist folder
            foreach(MusicPlaylist p in this.library)
            {
                string playlistNewLocation = newWorkArea + "\\" + p.id;
                DiskIO.CreateDirectory(playlistNewLocation);
                // determine music files to copy
                foreach(MusicFile file in p.GetMusicFilesInfo())
                {
                    // add music file
                    allFilesToCopy.Add(new FileCopier(file.file, playlistNewLocation + "\\" + file.title));
                    // change the file path to the new path for further library json saving
                    file.file = p.id+ "\\" + file.title;
                }
                // add playlist cover to copy
                allFilesToCopy.Add(new FileCopier(p.cover, playlistNewLocation + "\\cover.jpg"));
                // change the cover path to the new path for further library json saving
                p.cover = p.id + "\\cover.jpg";
                p.playlist = p.id + "\\index.m3u";
            }
            // save changed paths and music files into exported location
            this.SavePlaylistLibraryAtLocation(newWorkArea, "index.en.json");

            // copy all requested non-English langs
            foreach(string lang in _languages)
            {
                string abbriv = "index." + lang.Substring(0, 2).ToLower() + ".json";
                if(DiskIO.IsFileExist(ContentLocation,abbriv))
                {
                    AudioFolder temp = DiskIO.DeserializeAudioFolderFromFile(ContentLocation ,abbriv);
                    foreach(MusicPlaylist x in temp.library)
                    {
                        x.playlist = x.id + "\\index.m3u";
                        x.cover = x.id + "\\cover.jpg";
                    }
                    DiskIO.SaveAsJSONFile(temp, newWorkArea, abbriv);
                }
            }
            return allFilesToCopy.ToArray();
        }
    }
}
