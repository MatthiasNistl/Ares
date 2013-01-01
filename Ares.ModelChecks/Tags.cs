﻿/*
 Copyright (c) 2012 [Joerg Ruedenauer]
 
 This file is part of Ares.

 Ares is free software; you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation; either version 2 of the License, or
 (at your option) any later version.

 Ares is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with Ares; if not, write to the Free Software
 Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ares.ModelInfo
{
    class TagExtractionInfo
    {
        public IList<String> Files { get; set; }
        public bool Interpret { get; set; }
        public bool Album { get; set; }
        public bool Genre { get; set; }
        public bool Mood { get; set; }
        public int LanguageId { get; set; }
    }

    public class TagExtractor
    {
        private ProgressMonitor m_Monitor;
        private System.ComponentModel.BackgroundWorker m_Worker;
        private Action<bool> m_FinishedAction;

        public static void ExtractTags(System.Windows.Forms.Form parent, IList<String> files, int languageId, 
            bool interpret, bool album, bool genre, bool mood,
            Action<bool> finishedAction)
        {
            TagExtractor extractor = new TagExtractor();
            TagExtractionInfo info = new TagExtractionInfo() { Files = files, Interpret = interpret, Album = album, Genre = genre, Mood = mood, LanguageId = languageId };
            extractor.DoExtraction(parent, info, finishedAction);
        }

        private void m_Worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            m_Monitor.Close();
            if (e.Error != null)
            {
                System.Windows.Forms.MessageBox.Show(String.Format(StringResources.TagExtractionError, e.Error.Message), StringResources.Ares,
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            if (m_FinishedAction != null)
            {
                m_FinishedAction(e.Error == null);
            }
        }

        private void m_Worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            if (m_Monitor.Canceled)
            {
                m_Worker.CancelAsync();
            }
            else
            {
                m_Monitor.SetProgress(e.ProgressPercentage, e.UserState != null ? e.UserState.ToString() : String.Empty);
            }
        }

        private void DoExtraction(System.Windows.Forms.Form parent, TagExtractionInfo info, Action<bool> finishedAction)
        {
            m_FinishedAction = finishedAction;
            m_Worker = new System.ComponentModel.BackgroundWorker();
            m_Worker.WorkerReportsProgress = true;
            m_Worker.WorkerSupportsCancellation = true;
            m_Worker.DoWork += new System.ComponentModel.DoWorkEventHandler(m_Worker_DoWork);
            m_Worker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(m_Worker_ProgressChanged);
            m_Worker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(m_Worker_RunWorkerCompleted);
            m_Monitor = new ProgressMonitor(parent, StringResources.ExtractingTags);
            m_Worker.RunWorkerAsync(info);
        }

        private TagExtractor()
        {
        }

        private void m_Worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            TagExtractionInfo info = (TagExtractionInfo)e.Argument;
            
            Dictionary<String, int> tags = new Dictionary<string, int>();
            int interpretCatId = -1;
            int albumCatId = -1;
            int genreCatId = -1;
            int moodCatId = -1;

            List<String> interprets = new List<string>();
            List<String> albums = new List<string>();
            List<String> genres = new List<string>();
            List<String> moods = new List<string>();

            Dictionary<String, int> interpretTags = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
            Dictionary<String, int> albumTags = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
            Dictionary<String, int> genreTags = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
            Dictionary<String, int> moodTags = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);

            int lastPercent = 0;
            int count = 0;
            String basePath = Settings.Settings.Instance.MusicDirectory;
            foreach (String file in info.Files)
            {
                String path = System.IO.Path.Combine(basePath, file);
                Un4seen.Bass.AddOn.Tags.TAG_INFO tag = Un4seen.Bass.AddOn.Tags.BassTags.BASS_TAG_GetFromFile(path, true, true);
                if (tag != null)
                {
                    if (!String.IsNullOrEmpty(tag.artist))
                    {
                        interpretTags[tag.artist] = -1;
                    }
                    if (!String.IsNullOrEmpty(tag.album))
                    {
                        albumTags[tag.album] = -1;
                    }
                    if (!String.IsNullOrEmpty(tag.genre))
                    {
                        genreTags[tag.genre] = -1;
                    }
                    if (!String.IsNullOrEmpty(tag.mood))
                    {
                        moodTags[tag.mood] = -1;
                    }

                    interprets.Add(tag.artist != null ? tag.artist : String.Empty);
                    albums.Add(tag.album != null ? tag.album : String.Empty);
                    genres.Add(tag.genre != null ? tag.genre : String.Empty);
                    moods.Add(tag.mood != null ? tag.mood : String.Empty);
                }
                else
                {
                    interprets.Add(String.Empty);
                    albums.Add(String.Empty);
                    genres.Add(String.Empty);
                    moods.Add(String.Empty);
                }
                int percent = (++count) * 90 / info.Files.Count;
                if (percent > lastPercent)
                {
                    m_Worker.ReportProgress(percent, file);
                    lastPercent = percent;
                }
                if (m_Worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
            }

            m_Worker.ReportProgress(90, StringResources.AddingTags);

            var dbRead = Ares.Tags.TagsModule.GetTagsDB().GetReadInterfaceByLanguage(info.LanguageId);
            var dbWrite = Ares.Tags.TagsModule.GetTagsDB().GetWriteInterfaceByLanguage(info.LanguageId);
            var categories = dbRead.GetAllCategories();
            int tagCount = 0;
            if (info.Interpret)
                tagCount += interpretTags.Count;
            if (info.Album)
                tagCount += albumTags.Count;
            if (info.Genre)
                tagCount += genreTags.Count;
            if (info.Mood)
                tagCount += moodTags.Count;
            int currentCount = 0;
            if (info.Interpret && interpretTags.Count > 0)
            {
                var interpretCat = categories.FirstOrDefault((Ares.Tags.CategoryForLanguage cat) => { return cat.Name.Equals(StringResources.Interpret, StringComparison.CurrentCultureIgnoreCase); });
                if (interpretCat != null)
                {
                    interpretCatId = interpretCat.Id;
                }
                else
                {
                    interpretCatId = dbWrite.AddCategory(StringResources.Interpret);
                }
                AddTags(interpretTags, interpretCatId, info.LanguageId, currentCount, tagCount, ref lastPercent);
                currentCount += interpretTags.Count;
            }
            if (info.Album && albumTags.Count > 0)
            {
                var albumCat = categories.FirstOrDefault((Ares.Tags.CategoryForLanguage cat) => { return cat.Name.Equals(StringResources.Album, StringComparison.CurrentCultureIgnoreCase); });
                if (albumCat != null)
                {
                    albumCatId = albumCat.Id;
                }
                else
                {
                    albumCatId = dbWrite.AddCategory(StringResources.Album);
                }
                AddTags(albumTags, albumCatId, info.LanguageId, currentCount, tagCount, ref lastPercent);
                currentCount += albumTags.Count;
            }
            if (info.Genre && genreTags.Count > 0)
            {
                var genreCat = categories.FirstOrDefault((Ares.Tags.CategoryForLanguage cat) => { return cat.Name.Equals(StringResources.Genre, StringComparison.CurrentCultureIgnoreCase); });
                if (genreCat != null)
                {
                    genreCatId = genreCat.Id;
                }
                else
                {
                    genreCatId = dbWrite.AddCategory(StringResources.Genre);
                }
                AddTags(genreTags, genreCatId, info.LanguageId, currentCount, tagCount, ref lastPercent);
                currentCount += genreTags.Count;
            }
            if (info.Mood && moodTags.Count > 0)
            {
                var moodCat = categories.FirstOrDefault((Ares.Tags.CategoryForLanguage cat) => { return cat.Name.Equals(StringResources.Mood, StringComparison.CurrentCultureIgnoreCase); });
                if (moodCat != null)
                {
                    moodCatId = moodCat.Id;
                }
                else
                {
                    moodCatId = dbWrite.AddCategory(StringResources.Mood);
                }
                AddTags(moodTags, moodCatId, info.LanguageId, currentCount, tagCount, ref lastPercent);
                tagCount += moodTags.Count;
            }

            m_Worker.ReportProgress(99, StringResources.SettingFileTags);

            List<IList<int>> newTags = new List<IList<int>>();
            for (int i = 0; i < info.Files.Count; ++i)
            {
                HashSet<int> fileTags = new HashSet<int>();
                var existingTags = dbRead.GetTagsForFile(info.Files[i]);
                foreach (var existingTag in existingTags)
                {
                    fileTags.Add(existingTag.Id);
                }
                if (info.Interpret && !String.IsNullOrEmpty(interprets[i]))
                    fileTags.Add(interpretTags[interprets[i]]);
                if (info.Album && !String.IsNullOrEmpty(albums[i]))
                    fileTags.Add(albumTags[albums[i]]);
                if (info.Genre && !String.IsNullOrEmpty(genres[i]))
                    fileTags.Add(genreTags[genres[i]]);
                if (info.Mood && !String.IsNullOrEmpty(moods[i]))
                    fileTags.Add(moodTags[moods[i]]);
                newTags.Add(new List<int>(fileTags));
            }
            var dbWrite2 = Ares.Tags.TagsModule.GetTagsDB().WriteInterface;
            dbWrite2.SetFileTags(info.Files, newTags);

            m_Worker.ReportProgress(100);
        }

        private void AddTags(Dictionary<String, int> tags, int categoryId, int languageId, int currentCount, int totalCount, ref int lastPercent)
        {
            var dbRead = Ares.Tags.TagsModule.GetTagsDB().GetReadInterfaceByLanguage(languageId);
            var dbWrite = Ares.Tags.TagsModule.GetTagsDB().GetWriteInterfaceByLanguage(languageId);
            var existingTags = dbRead.GetAllTags(categoryId);
            List<String> keys = new List<string>(tags.Keys);
            foreach (String key in keys)
            {
                var existingTag = existingTags.FirstOrDefault((Ares.Tags.TagForLanguage tag) => { return tag.Name.Equals(key, StringComparison.CurrentCultureIgnoreCase); });
                if (existingTag != null)
                {
                    tags[key] = existingTag.Id;
                }
                else
                {
                    tags[key] = dbWrite.AddTag(categoryId, key);
                }
                int percent = (++currentCount) * 10 / totalCount + 90;
                if (percent > lastPercent)
                {
                    m_Worker.ReportProgress(percent, StringResources.AddingTags);
                    lastPercent = percent;
                }
            }
        }

    }
}