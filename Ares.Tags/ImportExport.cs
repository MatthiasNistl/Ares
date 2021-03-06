﻿/*
 Copyright (c) 2015  [Joerg Ruedenauer]
 
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
using System.Text;
using System.IO;

using System.Data.Common;

namespace Ares.Tags
{

    class FileExchange : FileIdentification
    {
        public String RelativePath { get; set; }
    }

    partial class SQLiteTagsDB
    {

        #region Export

        public void ExportDatabase(IList<String> filePaths, String targetFileName)
        {
            if (filePaths == null)
            {
                throw new ArgumentException("File paths must be given", "filePaths");
            }
            if (String.IsNullOrEmpty(targetFileName))
            {
                throw new ArgumentException("File name must be given", "targetFileName");
            }
            if (m_Connection == null)
            {
                throw new TagsDbException("No Connection to DB file!");
            }
            try
            {
                DoExportDatabase(filePaths, targetFileName);
            }
            catch (System.Data.DataException ex)
            {
                throw new TagsDbException(ex.Message, ex);
            }
            catch (DbException ex)
            {
                throw new TagsDbException(ex.Message, ex);
            }
        }

        public TagsExportedData<FileIdentification> ExportDatabaseForGlobalDB(IList<String> filePaths)
        {
            if (filePaths == null)
            {
                throw new ArgumentException("File paths must be given", "filePaths");
            }
            if (m_Connection == null)
            {
                throw new TagsDbException("No Connection to DB file!");
            }
            try
            {
                return CreateExportedData<FileIdentification>(filePaths, true);
            }
            catch (System.Data.DataException ex)
            {
                throw new TagsDbException(ex.Message, ex);
            }
            catch (DbException ex)
            {
                throw new TagsDbException(ex.Message, ex);
            }
        }

        public TagsExportedData<FileIdentification> ExportDatabaseForGlobalDB(IList<int> fileIds)
        {
            if (fileIds == null)
            {
                throw new ArgumentException("File ids must be given", "filePaths");
            }
            if (m_Connection == null)
            {
                throw new TagsDbException("No Connection to DB file!");
            }
            try
            {
                return CreateExportedData<FileIdentification>(fileIds, true);
            }
            catch (System.Data.DataException ex)
            {
                throw new TagsDbException(ex.Message, ex);
            }
            catch (DbException ex)
            {
                throw new TagsDbException(ex.Message, ex);
            }
        }

        // method from global database
        public TagsExportedData<FileIdentification> ExportDataForFiles(IList<FileIdentification> files, out int foundFiles)
        {
            if (files == null || files.Count == 0)
            {
                foundFiles = 0;
                return new TagsExportedData<FileIdentification>();
            }
            if (m_Connection == null)
            {
                throw new TagsDbException("No connection to DB file!");
            }
            try
            {
                return CreateExportedData(files, out foundFiles);
            }
            catch (System.Data.DataException ex)
            {
                throw new TagsDbException(ex.Message, ex);
            }
            catch (DbException ex)
            {
                throw new TagsDbException(ex.Message, ex);
            }
        }

        private void DoExportDatabase(IList<String> filePaths, String targetFileName)
        {
            TagsExportedData<FileExchange> data = CreateExportedData<FileExchange>(filePaths, false);
            WriteDataToFile(data, targetFileName);
        }

        private void WriteDataToFile(TagsExportedData<FileExchange> data, String targetFileName)
        {
            using (StreamWriter writer = new StreamWriter(targetFileName, false, Encoding.UTF8))
            {
                ServiceStack.Text.TypeSerializer.SerializeToWriter(data, writer);
                writer.Flush();
            }
        }

        private static readonly String GET_TRANSLATION_INFO = "SELECT {1}, {2} FROM {0} WHERE {0}.{3} = {4}";

        private static DbCommand CreateTranslationInfosCommand(String translationTable, String mainIdColumn, String languageIdColumn, String mainIdParam, 
            DbConnection connection, DbTransaction transaction)
        {
            return DbUtils.CreateDbCommand(String.Format(GET_TRANSLATION_INFO, translationTable, languageIdColumn, Schema.NAME_COLUMN, mainIdColumn, mainIdParam),
                connection, transaction);
        }


        private TagsExportedData<T> CreateExportedData<T>(IList<String> filePaths, bool excludeGlobalDBData) where T : FileIdentification
        {
            TagsExportedData<T> data = new TagsExportedData<T>();
            
            // Use temporary table with all file IDs. This is so we don't have to
            // use a "where File.Path in (........)" in each query or even manually
            // iterate over each file.
            // Use another temporary table which holds all tags which are either assigned
            // or removed from any of the files.
            using (DbTransaction transaction = m_Connection.BeginTransaction())
            {
                // no need to clear export tables: transaction will always be rolled back
                String moveCommand = String.Format("INSERT INTO {0} ({1}, {2}) SELECT {3}, {4} FROM {5} WHERE {4} = @FilePath",
                    Schema.FILEEXPORT_TABLE, Schema.FILE_COLUMN, Schema.PATH_COLUMN, Schema.ID_COLUMN, Schema.PATH_COLUMN, Schema.FILES_TABLE, Schema.PATH_COLUMN);
                using (DbCommand command = DbUtils.CreateDbCommand(moveCommand, m_Connection, transaction))
                {
                    DbParameter param = command.AddParameter("@FilePath", System.Data.DbType.String);
                    foreach (String file in filePaths)
                    {
                        param.Value = file;
                        command.ExecuteNonQuery();
                    }
                }

                DoCreateExportedData(data, transaction, excludeGlobalDBData);
                transaction.Rollback();
            }

            return data;
        }

        private TagsExportedData<T> CreateExportedData<T>(IList<int> fileIds, bool excludeGlobalDBData) where T : FileIdentification
        {
            TagsExportedData<T> data = new TagsExportedData<T>();

            // Use temporary table with all file IDs. This is so we don't have to
            // use a "where File.Path in (........)" in each query or even manually
            // iterate over each file.
            // Use another temporary table which holds all tags which are either assigned
            // or removed from any of the files.
            using (DbTransaction transaction = m_Connection.BeginTransaction())
            {
                // no need to clear export tables: transaction will always be rolled back
                String moveCommand = String.Format("INSERT INTO {0} ({1}, {2}) SELECT {3}, {4} FROM {5} WHERE {3} = @FileId",
                    Schema.FILEEXPORT_TABLE, Schema.FILE_COLUMN, Schema.PATH_COLUMN, Schema.ID_COLUMN, Schema.PATH_COLUMN, Schema.FILES_TABLE, Schema.ID_COLUMN);
                using (DbCommand command = DbUtils.CreateDbCommand(moveCommand, m_Connection, transaction))
                {
                    DbParameter param = command.AddParameter("@FileId", System.Data.DbType.Int64);
                    foreach (int fileId in fileIds)
                    {
                        param.Value = fileId;
                        command.ExecuteNonQuery();
                    }
                }

                DoCreateExportedData(data, transaction, excludeGlobalDBData);
                transaction.Rollback();
            }

            return data;
        }

        private void DoCreateExportedData<T>(TagsExportedData<T> data, DbTransaction transaction, bool excludeGlobalDBData) where T : FileIdentification
        {
            FindTagsForExport(transaction, excludeGlobalDBData);

            // file tags information
            String fileTagsInfo = String.Format("SELECT {0}.{1}, {0}.{2} FROM {0}, {3} WHERE {0}.{1} = {3}.{4}",
                Schema.FILETAGS_TABLE, Schema.FILE_COLUMN, Schema.TAG_COLUMN, Schema.FILEEXPORT_TABLE, Schema.FILE_COLUMN);
            if (excludeGlobalDBData)
            {
                fileTagsInfo += String.Format(" AND {0}.{1}!='{2}'", Schema.FILETAGS_TABLE, Schema.USER_COLUMN, Schema.GLOBAL_DB_USER);
            }
            fileTagsInfo += String.Format(" ORDER BY {0}.{1}", Schema.FILETAGS_TABLE, Schema.FILE_COLUMN);
            List<TagsForFileExchange> tagsForFileExchange = new List<TagsForFileExchange>();
            using (DbCommand fileTagsCommand = DbUtils.CreateDbCommand(fileTagsInfo, m_Connection, transaction))
            {
                using (DbDataReader reader = fileTagsCommand.ExecuteReader())
                {
                    TagsForFileExchange tagsForFiles = null;
                    while (reader.Read())
                    {
                        long fileId = reader.GetInt64(0);
                        if (tagsForFiles == null || fileId != tagsForFiles.FileId)
                        {
                            tagsForFiles = new TagsForFileExchange() { FileId = fileId, TagIds = new List<long>() };
                            tagsForFileExchange.Add(tagsForFiles);
                        }
                        tagsForFiles.TagIds.Add(reader.GetInt64(1));
                    }
                }
            }
            data.TagsForFiles = tagsForFileExchange;

            // removed tags information
            String removedTagsInfo = String.Format("SELECT {0}.{1}, {0}.{2} FROM {0}, {3} WHERE {0}.{1} = {3}.{4}",
                Schema.REMOVEDTAGS_TABLE, Schema.FILE_COLUMN, Schema.TAG_COLUMN, Schema.FILEEXPORT_TABLE, Schema.FILE_COLUMN);
            if (excludeGlobalDBData)
            {
                removedTagsInfo += String.Format(" AND {0}.{1}!='{2}'", Schema.REMOVEDTAGS_TABLE, Schema.USER_COLUMN, Schema.GLOBAL_DB_USER);
            }
            removedTagsInfo += String.Format(" ORDER BY {0}.{1}", Schema.REMOVEDTAGS_TABLE, Schema.FILE_COLUMN);
            List<TagsForFileExchange> removedTags = new List<TagsForFileExchange>();
            using (DbCommand removedTagsCommand = DbUtils.CreateDbCommand(removedTagsInfo, m_Connection, transaction))
            {
                using (DbDataReader reader = removedTagsCommand.ExecuteReader())
                {
                    TagsForFileExchange tagsForFiles = null;
                    while (reader.Read())
                    {
                        long fileId = reader.GetInt64(0);
                        if (tagsForFiles == null || fileId != tagsForFiles.FileId)
                        {
                            tagsForFiles = new TagsForFileExchange() { FileId = fileId, TagIds = new List<long>() };
                            removedTags.Add(tagsForFiles);
                        }
                        tagsForFiles.TagIds.Add(reader.GetInt64(1));
                    }
                }
            }
            data.RemovedTags = removedTags;

            FillExportedData(data, transaction, null);
        }

        private void FindTagsForExport(DbTransaction transaction, bool excludeGlobalDBData)
        {
            String moveCommand2 = String.Format("INSERT INTO {0} ({2}) SELECT DISTINCT {3}.{4} FROM {3},{5},{6} WHERE {3}.{4}={5}.{7} AND {5}.{8}={6}.{9}",
                Schema.TAGEXPORT_TABLE, Schema.ID_COLUMN, Schema.TAG_COLUMN, Schema.TAGS_TABLE, Schema.ID_COLUMN,
                Schema.FILETAGS_TABLE, Schema.FILEEXPORT_TABLE, Schema.TAG_COLUMN, Schema.FILE_COLUMN, Schema.FILE_COLUMN);
            if (excludeGlobalDBData)
            {
                moveCommand2 += String.Format(" AND {0}.{1}!='{2}'", Schema.FILETAGS_TABLE, Schema.USER_COLUMN, Schema.GLOBAL_DB_USER);
            }
            using (DbCommand command = DbUtils.CreateDbCommand(moveCommand2, m_Connection, transaction))
            {
                command.ExecuteNonQuery();
            }

            String moveCommand3 = String.Format("INSERT INTO {0} ({2}) SELECT DISTINCT {3}.{4} FROM {3},{5},{6} WHERE {3}.{4}={5}.{7} AND {5}.{8}={6}.{9}",
                Schema.TAGEXPORT_TABLE, Schema.ID_COLUMN, Schema.TAG_COLUMN, Schema.TAGS_TABLE, Schema.ID_COLUMN,
                Schema.REMOVEDTAGS_TABLE, Schema.FILEEXPORT_TABLE, Schema.TAG_COLUMN, Schema.FILE_COLUMN, Schema.FILE_COLUMN);
            if (excludeGlobalDBData)
            {
                moveCommand3 += String.Format(" AND {0}.{1}!='{2}'", Schema.REMOVEDTAGS_TABLE, Schema.USER_COLUMN, Schema.GLOBAL_DB_USER);
            }
            using (DbCommand command = DbUtils.CreateDbCommand(moveCommand3, m_Connection, transaction))
            {
                command.ExecuteNonQuery();
            }
        }

        TagsExportedData<FileIdentification> CreateExportedData(IList<FileIdentification> files, out int foundFiles)
        {
            TagsExportedData<FileIdentification> data = new TagsExportedData<FileIdentification>();
            foundFiles = 0;

            using (DbTransaction transaction = m_Connection.BeginTransaction())
            {
                Dictionary<long, long> fileIdMap = new Dictionary<long, long>();

                // find requested files
                String moveCommand = String.Format("INSERT INTO {0} ({1}, {2}) SELECT {3}, {4} FROM {5} WHERE {6} = @FileId",
                    Schema.FILEEXPORT_TABLE, Schema.FILE_COLUMN, Schema.PATH_COLUMN, Schema.ID_COLUMN, Schema.PATH_COLUMN, Schema.FILES_TABLE, Schema.ID_COLUMN);

                using (FileFinder finder = new FileFinder(m_Connection, transaction))
                {
                    using (DbCommand moveCmd = DbUtils.CreateDbCommand(moveCommand, m_Connection, transaction))
                    {
                        DbParameter moveParam = moveCmd.AddParameter("@FileId", System.Data.DbType.Int64);

                        foreach (FileIdentification file in files)
                        {
                            long id = finder.FindFileByIdentification(file, null, null);
                            // remember if found
                            if (id != -1)
                            {
                                fileIdMap[id] = file.Id;
                                moveParam.Value = id;
                                moveCmd.ExecuteNonQuery();
                                ++foundFiles;
                            }
                        }
                    }
                }

                // find tags for those files
                FindTagsForExport(transaction, false);

                String countAddsQuery = String.Format("SELECT count(*) FROM {0} WHERE {0}.{1}=@FileId1 AND {0}.{2}={3}.{4}",
                    Schema.FILETAGS_TABLE, Schema.FILE_COLUMN, Schema.TAG_COLUMN, Schema.TAGEXPORT_TABLE, Schema.TAG_COLUMN);
                String countRemovesQuery = String.Format("SELECT count(*) FROM {0} WHERE {0}.{1}=@FileId2 AND {0}.{2}={3}.{4}",
                    Schema.REMOVEDTAGS_TABLE, Schema.FILE_COLUMN, Schema.TAG_COLUMN, Schema.TAGEXPORT_TABLE, Schema.TAG_COLUMN);

                // file tags information
                String fileTagsInfo = String.Format("SELECT DISTINCT {0}.{1} FROM {0},{2} WHERE {2}.{3}=@FileId3 AND {0}.{1}={2}.{4} AND ("
                    + countAddsQuery + ") > (" + countRemovesQuery + ")", 
                    Schema.TAGEXPORT_TABLE, Schema.TAG_COLUMN, Schema.FILETAGS_TABLE, Schema.FILE_COLUMN, Schema.TAG_COLUMN);
                List<TagsForFileExchange> tagsForFileExchange = new List<TagsForFileExchange>();
                using (DbCommand fileTagsCommand = DbUtils.CreateDbCommand(fileTagsInfo, m_Connection, transaction))
                {
                    DbParameter fileParam1 = fileTagsCommand.AddParameter("@FileId1", System.Data.DbType.Int64);
                    DbParameter fileParam2 = fileTagsCommand.AddParameter("@FileId2", System.Data.DbType.Int64);
                    DbParameter fileParam3 = fileTagsCommand.AddParameter("@FileId3", System.Data.DbType.Int64);
                    foreach (var entry in fileIdMap)
                    {
                        fileParam1.Value = entry.Key;
                        fileParam2.Value = entry.Key;
                        fileParam3.Value = entry.Key;
                        TagsForFileExchange tagsForFile = new TagsForFileExchange() { FileId = entry.Value, TagIds = new List<long>() };
                        using (DbDataReader reader = fileTagsCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                long tagId = reader.GetInt64(0);
                                tagsForFile.TagIds.Add(tagId);
                            }
                        }
                        tagsForFileExchange.Add(tagsForFile);
                    }
                }
                data.TagsForFiles = tagsForFileExchange;

                // removed tags information
                String removedTagsInfo = String.Format("SELECT DISTINCT {0}.{1} FROM {0},{2} WHERE {2}.{3}=@FileId3 AND {0}.{1}={2}.{4} AND ("
                    + countAddsQuery + ") < (" + countRemovesQuery + ")",
                    Schema.TAGEXPORT_TABLE, Schema.TAG_COLUMN, Schema.REMOVEDTAGS_TABLE, Schema.FILE_COLUMN, Schema.TAG_COLUMN);
                List<TagsForFileExchange> removedTags = new List<TagsForFileExchange>();
                using (DbCommand fileTagsCommand = DbUtils.CreateDbCommand(removedTagsInfo, m_Connection, transaction))
                {
                    DbParameter fileParam1 = fileTagsCommand.AddParameter("@FileId1", System.Data.DbType.Int64);
                    DbParameter fileParam2 = fileTagsCommand.AddParameter("@FileId2", System.Data.DbType.Int64);
                    DbParameter fileParam3 = fileTagsCommand.AddParameter("@FileId3", System.Data.DbType.Int64);
                    foreach (var entry in fileIdMap)
                    {
                        fileParam1.Value = entry.Key;
                        fileParam2.Value = entry.Key;
                        fileParam3.Value = entry.Key;
                        TagsForFileExchange tagsForFile = new TagsForFileExchange() { FileId = entry.Value, TagIds = new List<long>() };
                        using (DbDataReader reader = fileTagsCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                long tagId = reader.GetInt64(0);
                                tagsForFile.TagIds.Add(tagId);
                            }
                        }
                        removedTags.Add(tagsForFile);
                    }
                }
                data.RemovedTags = removedTags;

                FillExportedData(data, transaction, fileIdMap);
                transaction.Rollback();
            }

            return data;
        }

        private void FillExportedData<T>(TagsExportedData<T> data, DbTransaction transaction, Dictionary<long, long> fileIdMap) where T : FileIdentification
        {
            // file information
            String fileInfo = String.Format("SELECT {6}.{0}, {6}.{1}, {6}.{2}, {6}.{3}, {6}.{4}, {6}.{5} FROM {6},{7} WHERE {6}.{0}={7}.{8}",
                Schema.ID_COLUMN, Schema.PATH_COLUMN, Schema.ARTIST_COLUMN, Schema.ALBUM_COLUMN, Schema.TITLE_COLUMN, Schema.ACOUST_ID_COLUMN,
                Schema.FILES_TABLE, Schema.FILEEXPORT_TABLE, Schema.FILE_COLUMN);
            List<T> fileExchange = new List<T>();
            using (DbCommand fileCommand = DbUtils.CreateDbCommand(fileInfo, m_Connection, transaction))
            {
                using (DbDataReader reader = fileCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        long id = reader.GetInt64(0);
                        if (fileIdMap != null && fileIdMap.ContainsKey(id))
                        {
                            id = fileIdMap[id];
                        }
                        if (fileExchange is List<FileExchange>)
                        {
                            fileExchange.Add((T)(FileIdentification)(new FileExchange()
                            {
                                Id = (int)id,
                                RelativePath = reader.GetString(1),
                                Artist = reader.GetStringOrEmpty(2),
                                Album = reader.GetStringOrEmpty(3),
                                Title = reader.GetStringOrEmpty(4),
                                AcoustId = reader.GetStringOrEmpty(5)
                            }));
                        }
                        else
                        {
                            fileExchange.Add((T)(new FileIdentification()
                            {
                                Id = (int)id,
                                Artist = reader.GetStringOrEmpty(2),
                                Album = reader.GetStringOrEmpty(3),
                                Title = reader.GetStringOrEmpty(4),
                                AcoustId = reader.GetStringOrEmpty(5)
                            }));
                        }
                    }
                }
            }
            data.Files = fileExchange;

            // tags information
            String tagsInfo = String.Format("SELECT DISTINCT {0}.{1}, {0}.{2} FROM {0}, {3} WHERE {0}.{1} = {3}.{4}",
                Schema.TAGS_TABLE, Schema.ID_COLUMN, Schema.CATEGORY_COLUMN, Schema.TAGEXPORT_TABLE, Schema.TAG_COLUMN);
            List<TagExchange> tags = new List<TagExchange>();
            using (DbCommand tagsInfoCommand = DbUtils.CreateDbCommand(tagsInfo, m_Connection, transaction))
            {
                using (DbDataReader reader = tagsInfoCommand.ExecuteReader())
                {
                    using (DbCommand translationInfoCommand = CreateTranslationInfosCommand(Schema.TAGNAMES_TABLE, Schema.TAG_COLUMN, Schema.LANGUAGE_COLUMN, "@TagId", m_Connection, transaction))
                    {
                        DbParameter param = translationInfoCommand.AddParameter("@TagId", System.Data.DbType.Int64);
                        while (reader.Read())
                        {
                            long tagId = reader.GetInt64(0);
                            param.Value = tagId;
                            List<TranslationExchange> translations = new List<TranslationExchange>();
                            using (DbDataReader reader2 = translationInfoCommand.ExecuteReader())
                            {
                                while (reader2.Read())
                                {
                                    translations.Add(new TranslationExchange() { LanguageId = reader2.GetInt64(0), Name = reader2.GetString(1) });
                                }
                            }
                            tags.Add(new TagExchange() { Id = tagId, CategoryId = reader.GetInt64(1), Names = translations });
                        }
                    }
                }
            }
            data.Tags = tags;

            // category information (no need for category table, just select distinct category ids from tags table)
            String categoryInfo = String.Format("SELECT DISTINCT {0}.{2} FROM {0}, {3} WHERE {0}.{1} = {3}.{4}",
                Schema.TAGS_TABLE, Schema.ID_COLUMN, Schema.CATEGORY_COLUMN, Schema.TAGEXPORT_TABLE, Schema.TAG_COLUMN);
            List<CategoryExchange> categories = new List<CategoryExchange>();
            using (DbCommand categoryInfoCommand = DbUtils.CreateDbCommand(categoryInfo, m_Connection, transaction))
            {
                using (DbDataReader reader = categoryInfoCommand.ExecuteReader())
                {
                    using (DbCommand translationInfoCommand = CreateTranslationInfosCommand(Schema.CATEGORYNAMES_TABLE, Schema.CATEGORY_COLUMN, Schema.LANGUAGE_COLUMN, "@CategoryId", m_Connection, transaction))
                    {
                        DbParameter param = translationInfoCommand.AddParameter("@CategoryId", System.Data.DbType.Int64);
                        while (reader.Read())
                        {
                            long categoryId = reader.GetInt64(0);
                            param.Value = categoryId;
                            List<TranslationExchange> translations = new List<TranslationExchange>();
                            using (DbDataReader reader2 = translationInfoCommand.ExecuteReader())
                            {
                                while (reader2.Read())
                                {
                                    translations.Add(new TranslationExchange() { LanguageId = reader2.GetInt64(0), Name = reader2.GetString(1) });
                                }
                            }
                            categories.Add(new CategoryExchange() { Id = categoryId, Names = translations });
                        }
                    }
                }
            }
            data.Categories = categories;

            // language information: always simply add all languages, those are few anyway
            String languageInfo = String.Format("SELECT {0}.{1}, {0}.{2}, {3}.{4}, {3}.{5} FROM {0}, {3} WHERE {0}.{1} = {3}.{6} ORDER BY {0}.{1}",
                Schema.LANGUAGE_TABLE, Schema.ID_COLUMN, Schema.LC_COLUMN, Schema.LANGUAGENAMES_TABLE, Schema.LANGUAGE_OF_NAME_COLUMN, Schema.NAME_COLUMN, Schema.NAMED_LANGUAGE_COLUMN);
            List<LanguageExchange> languages = new List<LanguageExchange>();
            using (DbCommand languageInfoCommand = DbUtils.CreateDbCommand(languageInfo, m_Connection, transaction))
            {
                using (DbDataReader reader = languageInfoCommand.ExecuteReader())
                {
                    LanguageExchange language = null;
                    while (reader.Read())
                    {
                        long langId = reader.GetInt64(0);
                        if (language == null || language.Id != langId)
                        {
                            language = new LanguageExchange() { Id = langId, ISO6391Code = reader.GetString(1), Names = new List<TranslationExchange>() };
                            languages.Add(language);
                        }
                        language.Names.Add(new TranslationExchange() { LanguageId = reader.GetInt64(2), Name = reader.GetString(3) });
                    }
                }
            }
            data.Languages = languages;
        }

        #endregion

        #region Import

        public void ImportDatabase(string filePath, TextWriter logStream)
        {
            if (String.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File name must be given", "filePath");
            }
            if (m_Connection == null)
            {
                throw new TagsDbException("No Connection to DB file!");
            }
            if (logStream == null)
            {
                // use dummy
                logStream = new StringWriter();
            }
            try
            {
                DoImportDatabase(filePath, logStream);
            }
            catch (System.Runtime.Serialization.SerializationException ex)
            {
                throw new TagsDbException(ex.Message, ex);
            }
            catch (System.Data.DataException ex)
            {
                throw new TagsDbException(ex.Message, ex);
            }
            catch (DbException ex)
            {
                throw new TagsDbException(ex.Message, ex);
            }
        }

        public void ImportDataFromGlobalDB(TagsExportedData<FileIdentification> data, IList<String> filesToMatch, TextWriter logStream)
        {
            if (data == null)
                return;
            if (logStream == null)
            {
                // use dummy
                logStream = new StringWriter();
            }
            try
            {
                ImportExportedData(data, filesToMatch, logStream, Schema.GLOBAL_DB_USER);
            }
            catch (System.Runtime.Serialization.SerializationException ex)
            {
                throw new TagsDbException(ex.Message, ex);
            }
            catch (System.Data.DataException ex)
            {
                throw new TagsDbException(ex.Message, ex);
            }
            catch (DbException ex)
            {
                throw new TagsDbException(ex.Message, ex);
            }
        }

        // method for global DB
        public void ImportDataFromClient(TagsExportedData<FileIdentification> data, String userId, System.IO.TextWriter logStream, out int nrOfNewFiles, out int nrOfNewTags)
        {
            nrOfNewFiles = 0;
            nrOfNewTags = 0;
            if (data == null)
                return;
            if (logStream == null)
            {
                // use dummy
                logStream = new StringWriter();
            }
            if (String.IsNullOrEmpty(userId))
            {
                return;
            }
            try
            {
                ImportDataIntoGlobalDB(data, logStream, userId, out nrOfNewFiles, out nrOfNewTags);
            }
            catch (System.Data.DataException ex)
            {
                throw new TagsDbException(ex.Message, ex);
            }
            catch (DbException ex)
            {
                throw new TagsDbException(ex.Message, ex);
            }
        }

        private void DoImportDatabase(String filePath, TextWriter logStream)
        {
            TagsExportedData<FileExchange> data = ReadTagsExportedData(filePath);
            ImportExportedData(data, null, logStream, System.IO.Path.GetFileName(filePath));
        }

        private TagsExportedData<FileExchange> ReadTagsExportedData(String filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var importedData = ServiceStack.Text.TypeSerializer.DeserializeFromStream<TagsExportedData<FileExchange>>(stream);
                return importedData;
            }
        }

        private void ImportExportedData<T>(TagsExportedData<T> data, IList<String> filesToMatch, TextWriter logStream, String user) where T : FileIdentification
        {
            if (data == null)
                return;
            using (DbTransaction transaction = m_Connection.BeginTransaction())
            {
                if (data.Languages == null)
                    return;
                if (data.Languages.Count == 0)
                    return;

                LocalDBImporter helper = new LocalDBImporter(this, m_Connection, transaction, logStream);
                helper.ImportExportedData(data, filesToMatch, user);

                transaction.Commit();
            }            
        }

        private void ImportDataIntoGlobalDB(TagsExportedData<FileIdentification> data, TextWriter logStream, String user, out int nrOfNewFiles, out int nrOfNewTags)
        {
            nrOfNewFiles = 0;
            nrOfNewTags = 0;
            if (data == null)
                return;
            using (DbTransaction transaction = m_Connection.BeginTransaction())
            {
                if (data.Languages == null)
                    return;
                if (data.Languages.Count == 0)
                    return;

                GlobalDBImporter helper = new GlobalDBImporter(this, m_Connection, transaction, logStream);
                helper.ImportExportedData(data, null, user, out nrOfNewFiles, out nrOfNewTags);

                transaction.Commit();
            }
        }

        // helper class mainly to avoid passing many parameters around
        private abstract class ImportHelperBase
        {
            protected SQLiteTagsDB m_TagsDb;
            protected DbConnection m_Connection;
            protected TextWriter m_LogStream;
            protected DbTransaction m_Transaction;
            protected Dictionary<long, long> m_LanguageMap = new Dictionary<long, long>();
            protected Dictionary<long, long> m_CategoriesMap = new Dictionary<long, long>();
            protected Dictionary<long, long> m_TagsMap = new Dictionary<long, long>();
            protected Dictionary<long, long> m_FilesMap = new Dictionary<long, long>();

            protected ImportHelperBase(SQLiteTagsDB tagsDB, DbConnection connection, DbTransaction transaction, TextWriter logStream)
            {
                m_TagsDb = tagsDB;
                m_LogStream = logStream;
                m_Transaction = transaction;
                m_Connection = connection;
            }

            public void ImportExportedData<T>(TagsExportedData<T> data, IList<String> filesToMatch, String user) where T : FileIdentification
            {
                int newFiles, newTags;
                ImportExportedData(data, filesToMatch, user, out newFiles, out newTags);
            }

            public void ImportExportedData<T>(TagsExportedData<T> data, IList<String> filesToMatch, String user, out int nrOfNewFiles, out int nrOfNewTags) where T : FileIdentification
            {
                ImportLanguages(data.Languages);
                ImportCategories(data.Categories);
                ImportTags(data.Tags, out nrOfNewTags);
                ImportFiles(data.Files, filesToMatch, user, out nrOfNewFiles);
                ImportFileTags(data.TagsForFiles, user);
                ImportRemovedTags(data.RemovedTags, user);
            }

            protected abstract bool AllowEmptyPathInserts { get; }

            private void ImportLanguages(List<LanguageExchange> languages)
            {
                // first add / find languages itself
                foreach (LanguageExchange le in languages)
                {
                    m_LanguageMap[le.Id] = FindOrImportLanguage(le);
                }
                // then add the translation (needs Ids of added languages)
                foreach (LanguageExchange le in languages)
                {
                    ImportLanguageTranslations(le);
                }
            }

            private long FindOrImportLanguage(LanguageExchange le)
            {
                long id;
                if (TagsTranslations.DoGetIdOfLanguage(le.ISO6391Code, out id, m_Connection, m_Transaction))
                {
                    m_LogStream.WriteLine(String.Format("Found language {0} (imported id {1}) with id {2}", le.ISO6391Code, le.Id, id));
                    return id;
                }
                else
                {
                    String name = String.Empty;
                    if (le.Names != null)
                    {
                        foreach (TranslationExchange translation in le.Names)
                        {
                            if (String.IsNullOrEmpty(translation.Name))
                            {
                                continue;
                            }
                            if (translation.LanguageId == le.Id)
                            {
                                name = translation.Name;
                                break;
                            }
                        }
                    }
                    if (name.Length > 0)
                    {
                        TagsTranslations translations = new TagsTranslations(m_Connection);
                        int res = translations.DoAddLanguage(le.ISO6391Code, name, m_Transaction);
                        m_LogStream.WriteLine(String.Format("Insert new language {0} (imported id {1}) with id {2}", le.ISO6391Code, le.Id, res));
                        return res;
                    }
                    else
                    {
                        throw new TagsDbException("A language must have a name in its own language");
                    }
                }
            }

            private void ImportLanguageTranslations(LanguageExchange le)
            {
                long languageId = m_LanguageMap[le.Id];
                ImportTranslations(le.Names, languageId, Schema.LANGUAGENAMES_TABLE, Schema.NAMED_LANGUAGE_COLUMN, Schema.LANGUAGE_OF_NAME_COLUMN, "language");
            }

            private void ImportCategories(List<CategoryExchange> categories)
            {
                if (categories == null)
                    return;
                foreach (CategoryExchange category in categories)
                {
                    long catId;
                    if (FindOrAddCategory(category, out catId))
                    {
                        m_CategoriesMap[category.Id] = catId;
                        ImportCategoryTranslations(category);
                    }
                }
            }

            private bool FindOrAddCategory(CategoryExchange category, out long catId)
            {
                // first try to find an existing category
                HashSet<long> existingIds = new HashSet<long>();
                String nameQueryString = String.Format("SELECT {0} FROM {1} WHERE {2}=@LangId AND {3}=@Name",
                    Schema.CATEGORY_COLUMN, Schema.CATEGORYNAMES_TABLE, Schema.LANGUAGE_COLUMN, Schema.NAME_COLUMN);
                using (DbCommand command = DbUtils.CreateDbCommand(nameQueryString, m_Connection, m_Transaction))
                {
                    DbParameter langParam = command.AddParameter("@LangId", System.Data.DbType.Int64);
                    DbParameter nameParam = command.AddParameter("@Name", System.Data.DbType.String);
                    if (category.Names != null)
                    {
                        foreach (TranslationExchange translation in category.Names)
                        {
                            if (String.IsNullOrEmpty(translation.Name))
                            {
                                m_LogStream.WriteLine(String.Format("WARNING: Ignoring empty name for category {0} translation into language {1}", category.Id, translation.LanguageId));
                                continue;
                            }
                            if (m_LanguageMap.ContainsKey(translation.LanguageId))
                            {
                                langParam.Value = m_LanguageMap[translation.LanguageId];
                                nameParam.Value = translation.Name;
                                Object val = command.ExecuteScalar();
                                if (val != null)
                                {
                                    existingIds.Add((long)val);
                                }
                            }
                            else
                            {
                                m_LogStream.WriteLine(String.Format("WARNING: Language id {0} for category translation '{1}' not found", translation.LanguageId, translation.Name));
                            }
                        }
                    }
                }
                if (existingIds.Count == 1)
                {
                    catId = existingIds.ToList()[0];
                    m_LogStream.WriteLine(String.Format("Found existing category {0} for imported category {1}", catId, category.Id));
                }
                else if (existingIds.Count > 1)
                {
                    catId = existingIds.ToList()[0];
                    m_LogStream.WriteLine(String.Format("WARNING: Found {0} existing categories for imported category {1}, using {2}", existingIds.Count, category.Id, catId));
                }
                else
                {
                    // no existing category found, insert a new one
                    String name = String.Empty;
                    long langId = 0;
                    if (category.Names != null)
                    {
                        foreach (TranslationExchange translation in category.Names)
                        {
                            if (String.IsNullOrEmpty(translation.Name))
                            {
                                continue;
                            }
                            if (m_LanguageMap.ContainsKey(translation.LanguageId))
                            {
                                name = translation.Name;
                                langId = m_LanguageMap[translation.LanguageId];
                                break;
                            }
                        }
                    }
                    if (name.Length > 0)
                    {
                        LanguageWriting writing = (LanguageWriting)m_TagsDb.GetWriteInterfaceByLanguage((int)langId);
                        catId = writing.DoAddCategory(name, m_Transaction);
                        m_LogStream.WriteLine(String.Format("Added category {0} (language {1}, new Id {2}) for Id {3}", name, langId, catId, category.Id));
                        m_CategoriesMap[category.Id] = catId;
                    }
                    else
                    {
                        m_LogStream.WriteLine(String.Format("WARNING: No usable translation found for category {0}; ignoring category", category.Id));
                        catId = 0;
                        return false;
                    }
                }
                return true;
            }

            private void ImportCategoryTranslations(CategoryExchange ce)
            {
                long categoryId = m_CategoriesMap[ce.Id];
                ImportTranslations(ce.Names, categoryId, Schema.CATEGORYNAMES_TABLE, Schema.CATEGORY_COLUMN, Schema.LANGUAGE_COLUMN, "category");
            }

            private void ImportTags(List<TagExchange> tags, out int nrOfNewTags)
            {
                nrOfNewTags = 0;
                if (tags == null)
                    return;
                foreach (TagExchange tag in tags)
                {
                    long tagId;
                    if (FindOrAddTag(tag, out tagId, ref nrOfNewTags))
                    {
                        m_TagsMap[tag.Id] = tagId;
                        ImportTagTranslations(tag);
                    }
                }
            }

            private bool FindOrAddTag(TagExchange tag, out long tagId, ref int nrOfNewTags)
            {
                // first try to find an existing tag

                // get all tags which have a name in a language which matches one of the translations for the imported tag
                Dictionary<long, long> existingIds = new Dictionary<long, long>();
                String nameQueryString = String.Format("SELECT {0}.{1}, {0}.{2} FROM {0}, {3} WHERE {0}.{1}={3}.{4} AND {3}.{5}=@LangId AND {3}.{6}=@Name",
                    Schema.TAGS_TABLE, Schema.ID_COLUMN, Schema.CATEGORY_COLUMN, Schema.TAGNAMES_TABLE, Schema.TAG_COLUMN, Schema.LANGUAGE_COLUMN, Schema.NAME_COLUMN);
                using (DbCommand command = DbUtils.CreateDbCommand(nameQueryString, m_Connection, m_Transaction))
                {
                    DbParameter langParam = command.AddParameter("@LangId", System.Data.DbType.Int64);
                    DbParameter nameParam = command.AddParameter("@Name", System.Data.DbType.String);
                    if (tag.Names != null)
                    {
                        foreach (TranslationExchange translation in tag.Names)
                        {
                            if (String.IsNullOrEmpty(translation.Name))
                            {
                                m_LogStream.WriteLine(String.Format("WARNING: Ignoring empty name for tag {0} translation into language {1}", tag.Id, translation.LanguageId));
                                continue;
                            }
                            if (m_LanguageMap.ContainsKey(translation.LanguageId))
                            {
                                langParam.Value = m_LanguageMap[translation.LanguageId];
                                nameParam.Value = translation.Name;
                                using (DbDataReader reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        existingIds[reader.GetInt64(0)] = reader.GetInt64(1);
                                    }
                                }
                            }
                            else
                            {
                                m_LogStream.WriteLine(String.Format("WARNING: Language id {0} for tag translation '{1}' not found", translation.LanguageId, translation.Name));
                            }
                        }
                    }
                }

                // check the categories of the found tags
                if (m_CategoriesMap.ContainsKey(tag.CategoryId))
                {
                    long categoryId = m_CategoriesMap[tag.CategoryId];
                    int countWithCorrectCategory = existingIds.Count((KeyValuePair<long, long> entry) => { return entry.Value == categoryId; });
                    if (countWithCorrectCategory > 0)
                    {
                        tagId = existingIds.First((KeyValuePair<long, long> entry) => { return entry.Value == categoryId; }).Key;
                        if (existingIds.Count > countWithCorrectCategory)
                        {
                            m_LogStream.WriteLine(String.Format("Found {0} existing tags for tag {1}, {2} with matching category; dismissing the rest",
                                existingIds.Count, tag.Id, countWithCorrectCategory));
                        }
                        if (countWithCorrectCategory > 1)
                        {
                            m_LogStream.WriteLine(String.Format("Found {0} matching tags for imported tag {1}, using {2}", countWithCorrectCategory, tag.Id, tagId));
                        }
                        else
                        {
                            m_LogStream.WriteLine(String.Format("Found existing tag {0} for imported tag {1}", tagId, tag.Id));
                        }
                        return true;
                    }
                    else if (existingIds.Count == 1)
                    {
                        m_LogStream.WriteLine(String.Format("Found an existing tag for imported tag {0}, but with different category. Adding tag with new category.", tag.Id));
                    }
                    else if (existingIds.Count > 1)
                    {
                        m_LogStream.WriteLine(String.Format("Found {0} existing tags for imported tag {1}, but with different categories. Adding tag with new category.",
                            existingIds.Count, tag.Id));
                    }
                }
                else if (existingIds.Count == 1)
                {
                    var entry = existingIds.ToList()[0];
                    tagId = entry.Key;
                    m_LogStream.WriteLine(String.Format("WARNING: Unknown category for imported tag {0}. Using found tag {1} with category {2}.", tag.Id, entry.Key, entry.Value));
                    return true;
                }
                else if (existingIds.Count > 1)
                {
                    var entry = existingIds.ToList()[0];
                    tagId = entry.Key;
                    m_LogStream.WriteLine(String.Format("WARNING: Unknown category for imported tag {0}. Found {1} matching tags; using found tag {2} with category {3}.", 
                        tag.Id, existingIds.Count, entry.Key, entry.Value));
                    return true;
                }

                // no existing tag found, insert a new one
                if (!m_CategoriesMap.ContainsKey(tag.CategoryId))
                {
                    m_LogStream.WriteLine(String.Format("WARNING: Unknown category {0} for imported tag {1} and no matching tags found. Skipping tag.", tag.CategoryId, tag.Id));
                    tagId = 0;
                    return false;
                }
                String name = String.Empty;
                long langId = 0;
                if (tag.Names != null)
                {
                    foreach (TranslationExchange translation in tag.Names)
                    {
                        if (String.IsNullOrEmpty(translation.Name))
                        {
                            continue;
                        }
                        if (m_LanguageMap.ContainsKey(translation.LanguageId))
                        {
                            name = translation.Name;
                            langId = m_LanguageMap[translation.LanguageId];
                            break;
                        }
                    }
                }
                if (name.Length > 0)
                {
                    LanguageWriting writing = (LanguageWriting)m_TagsDb.GetWriteInterfaceByLanguage((int)langId);
                    tagId = writing.DoAddTag((int)m_CategoriesMap[tag.CategoryId], name, m_Transaction);
                    m_LogStream.WriteLine(String.Format("Added tag {0} (language {1}, new Id {2}) for Id {3}", name, langId, tagId, tag.Id));
                    ++nrOfNewTags;
                    return true;
                }
                else
                {
                    m_LogStream.WriteLine(String.Format("WARNING: No usable translation found for tag {0}; ignoring tag", tag.Id));
                    tagId = 0;
                    return false;
                }
            }

            private void ImportTagTranslations(TagExchange te)
            {
                long tagId = m_TagsMap[te.Id];
                ImportTranslations(te.Names, tagId, Schema.TAGNAMES_TABLE, Schema.TAG_COLUMN, Schema.LANGUAGE_COLUMN, "tag");
            }

            private void ImportFiles<T>(List<T> files, IList<String> filesToMatch, String user, out int nrOfNewFiles) where T : FileIdentification
            {
                nrOfNewFiles = 0;
                if (files == null)
                    return;
                String fileQuery = String.Format("SELECT {0} FROM {1} WHERE {2}=@FilePath", Schema.ID_COLUMN, Schema.FILES_TABLE, Schema.PATH_COLUMN);
                String fileQueryById = String.Format("SELECT {0} FROM {1} WHERE {2}=@Id", Schema.ID_COLUMN, Schema.FILES_TABLE, Schema.ID_COLUMN); // just to check existance
                String insertString = String.Format("INSERT INTO {0} ({1}, {2}, {3}, {4}, {5}, {6}) VALUES (@Id, @Path, @Artist, @Album, @Title, @AcoustId)", 
                    Schema.FILES_TABLE, Schema.ID_COLUMN, Schema.PATH_COLUMN, Schema.ARTIST_COLUMN, Schema.ALBUM_COLUMN, Schema.TITLE_COLUMN, Schema.ACOUST_ID_COLUMN);

                using (FileFinder finder = new FileFinder(m_Connection, m_Transaction))
                {
                    using (DbCommand command = DbUtils.CreateDbCommand(fileQuery, m_Connection, m_Transaction),
                           command3 = DbUtils.CreateDbCommand(fileQueryById, m_Connection, m_Transaction),
                           insertCommand = DbUtils.CreateDbCommand(insertString, m_Connection, m_Transaction))
                    {
                        DbParameter param = command.AddParameter("@FilePath", System.Data.DbType.String);
                        DbParameter param2 = command.AddParameter("@AcoustId", System.Data.DbType.String);
                        DbParameter param3 = command.AddParameter("@Id", System.Data.DbType.Int64);
                        insertCommand.AddParameterWithValue("@Id", DBNull.Value);
                        DbParameter pathInsertParam = insertCommand.AddParameter("@Path", System.Data.DbType.String);
                        DbParameter artistInsertParam = insertCommand.AddParameter("@Artist", System.Data.DbType.String);
                        DbParameter albumInsertParam = insertCommand.AddParameter("@Album", System.Data.DbType.String);
                        DbParameter titleInsertParam = insertCommand.AddParameter("@Title", System.Data.DbType.String);
                        DbParameter acoustIdInsertParam = insertCommand.AddParameter("@AcoustId", System.Data.DbType.String);
                        foreach (FileIdentification file in files)
                        {
                            FileExchange fileExchange = file as FileExchange;
                            String filePath = String.Empty;
                            if (fileExchange != null && !String.IsNullOrEmpty(fileExchange.RelativePath))
                            {
                                filePath = fileExchange.RelativePath.Replace('\\', System.IO.Path.DirectorySeparatorChar);
                            }
                            Object existingId = null;
                            if (user != Schema.GLOBAL_DB_USER && !String.IsNullOrEmpty(filePath))
                            {
                                param.Value = filePath;
                                existingId = command.ExecuteScalar();
                            }
                            else if (user == Schema.GLOBAL_DB_USER && file.Id != -1)
                            {
                                param3.Value = file.Id;
                                existingId = command.ExecuteScalar();
                            }
                            // not found? Try to find through acoustId or artist / title.
                            if (existingId == null)
                            {
                                long foundId = finder.FindFileByIdentification(file, filesToMatch, m_LogStream);
                                if (foundId != -1)
                                    existingId = foundId;
                            }

                            // found?
                            if (existingId != null)
                            {
                                if (user != Schema.GLOBAL_DB_USER && !String.IsNullOrEmpty(filePath))
                                {
                                    m_LogStream.WriteLine(String.Format("Found existing id {0} for imported file {1}", existingId, filePath));
                                }
                                else if (user == Schema.GLOBAL_DB_USER && file.Id != -1)
                                {
                                    m_LogStream.WriteLine(String.Format("Found existing file {0} in database", existingId));
                                }
                                else
                                {
                                    m_LogStream.WriteLine(String.Format("Found existing file {0} for acoust Id {1}", existingId, file.AcoustId));
                                }
                                m_FilesMap[file.Id] = (long)existingId;
                                UpdateFile((long)existingId, file);
                            }
                            else if (user != Schema.GLOBAL_DB_USER && (!String.IsNullOrEmpty(filePath) || AllowEmptyPathInserts))
                            {
                                pathInsertParam.Value = String.IsNullOrEmpty(filePath) ? String.Empty : filePath;
                                artistInsertParam.Value = String.IsNullOrEmpty(file.Artist) ? String.Empty : file.Artist;
                                albumInsertParam.Value = String.IsNullOrEmpty(file.Album) ? String.Empty : file.Album;
                                titleInsertParam.Value = String.IsNullOrEmpty(file.Title) ? String.Empty : file.Title;
                                acoustIdInsertParam.Value = String.IsNullOrEmpty(file.AcoustId) ? String.Empty : file.AcoustId;
                                insertCommand.ExecuteNonQuery();
                                long id = m_Connection.LastInsertRowId();
                                m_FilesMap[file.Id] = id;
                                m_LogStream.WriteLine(String.Format("Insert new file {0} with id {1} (imported id {2})", filePath, id, file.Id));
                                ++nrOfNewFiles;
                            }
                            else
                            {
                                if (AllowEmptyPathInserts)
                                {
                                    m_LogStream.WriteLine("WARNING: Ignoring imported file {0}, acoust Id or artist / title not found in database", file.Id);
                                }
                                else
                                {
                                    m_LogStream.WriteLine("WARNING: Ignoring file {0} with missing path", file.Id);
                                }
                            }
                        }
                    }
                }
            }

            private void UpdateFile(long existingId, FileIdentification file)
            {
                bool needsUpdate = false;
                String newArtist, newAlbum, newTitle, newAcoustId;

                String query = String.Format("SELECT {0}, {1}, {2}, {3} FROM {4} WHERE {5}=@Id",
                    Schema.ARTIST_COLUMN, Schema.ALBUM_COLUMN, Schema.TITLE_COLUMN, Schema.ACOUST_ID_COLUMN, Schema.FILES_TABLE, Schema.ID_COLUMN);
                using (DbCommand queryCommand = DbUtils.CreateDbCommand(query, m_Connection, m_Transaction))
                {
                    queryCommand.AddParameterWithValue("@Id", existingId);
                    using (DbDataReader reader = queryCommand.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            m_LogStream.WriteLine("WARNING: file entry to update not found. Concurrent transaction?");
                            return;
                        }
                        String oldArtist = reader.GetStringOrEmpty(0);
                        // Update only if old artist is empty and new artist isn't empty
                        newArtist = String.IsNullOrEmpty(file.Artist) || !String.IsNullOrEmpty(oldArtist) ? oldArtist : file.Artist;
                        if (newArtist != oldArtist)
                        {
                            m_LogStream.WriteLine(String.Format("Update Artist of file {0} to {1}", existingId, newArtist));
                            needsUpdate = true;
                        }
                        String oldAlbum = reader.GetStringOrEmpty(1);
                        newAlbum = String.IsNullOrEmpty(file.Album) || !String.IsNullOrEmpty(oldAlbum) ? oldAlbum : file.Album;
                        if (newAlbum != oldAlbum)
                        {
                            m_LogStream.WriteLine(String.Format("Update Album of file {0} to {1}", existingId, newAlbum));
                            needsUpdate = true;
                        }
                        String oldTitle = reader.GetStringOrEmpty(2);
                        newTitle = String.IsNullOrEmpty(file.Title) || !String.IsNullOrEmpty(oldTitle) ? oldTitle : file.Title;
                        if (newTitle != oldTitle)
                        {
                            m_LogStream.WriteLine(String.Format("Update Title of file {0} to {1}", existingId, newTitle));
                            needsUpdate = true;
                        }
                        String oldAcoustId = reader.GetStringOrEmpty(3);
                        if (!String.IsNullOrEmpty(oldAcoustId) && !String.IsNullOrEmpty(file.AcoustId) && file.AcoustId != oldAcoustId)
                        {
                            m_LogStream.WriteLine(String.Format("WARNING: acoust id of file {0} differs!", existingId));
                        }
                        newAcoustId = String.IsNullOrEmpty(file.AcoustId) || !String.IsNullOrEmpty(oldAcoustId) ? oldAcoustId : file.AcoustId;
                        if (newAcoustId != oldAcoustId)
                        {
                            m_LogStream.WriteLine(String.Format("Update acoust id of file {0}", existingId));
                            needsUpdate = true;
                        }
                    }
                }
                if (!needsUpdate)
                    return;

                String updateString = String.Format("UPDATE {0} SET {1}=@Artist, {2}=@Album, {3}=@Title, {4}=@AcoustId WHERE {5}=@Id",
                    Schema.FILES_TABLE, Schema.ARTIST_COLUMN, Schema.ALBUM_COLUMN, Schema.TITLE_COLUMN, Schema.ACOUST_ID_COLUMN, Schema.ID_COLUMN);
                using (DbCommand command = DbUtils.CreateDbCommand(updateString, m_Connection, m_Transaction))
                {
                    command.AddParameterWithValue("@Artist", newArtist);
                    command.AddParameterWithValue("@Album", newAlbum);
                    command.AddParameterWithValue("@Title", newTitle);
                    command.AddParameterWithValue("@AcoustId", newAcoustId);
                    command.AddParameterWithValue("@Id", existingId);
                    command.ExecuteNonQuery();
                }
            }

            protected abstract void ImportFileTags(List<TagsForFileExchange> tags, String user);
            protected abstract void ImportRemovedTags(List<TagsForFileExchange> tags, String user);

            private void ImportTranslations(List<TranslationExchange> names, long id, String tableName, String refColumnName, String langColumnName, String typeNameForLog)
            {
                if (names == null || names.Count == 0)
                    return;
                var translations = TagsTranslations.DoGetTranslations(tableName, refColumnName, langColumnName, id, m_Connection, m_Transaction);
                foreach (TranslationExchange translation in names)
                {
                    if (m_LanguageMap.ContainsKey(translation.LanguageId))
                    {
                        long realId = m_LanguageMap[translation.LanguageId];
                        if (translations.ContainsKey((int)realId))
                        {
                            if (String.IsNullOrEmpty(translation.Name))
                            {
                                m_LogStream.WriteLine(String.Format("WARNING: Ignoring empty name for {2} {0} translation into language {1}", id, translation.LanguageId, typeNameForLog));
                                continue;
                            }
                            String oldName = translations[(int)realId];
                            if (oldName.Equals(translation.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                m_LogStream.WriteLine(String.Format("Translation '{0}' for {2} {1} already exists", oldName, id, typeNameForLog));
                                continue;
                            }
                            else
                            {
                                m_LogStream.WriteLine(String.Format("Changing {4} translation for language {0} into language {1} from '{2}' to '{3}'",
                                    id, realId, oldName, translation.Name, typeNameForLog));
                            }
                        }
                        else
                        {
                            m_LogStream.WriteLine(String.Format("Adding {3} translation '{0}' for language {1} into language {2}", translation.Name, id, realId, typeNameForLog));
                        }
                        TagsTranslations.DoSetTranslation(m_Connection, tableName, refColumnName, langColumnName, id, realId, translation.Name, m_Transaction);
                    }
                    else
                    {
                        m_LogStream.WriteLine(String.Format("WARNING: Language id {0} for {2} translation '{1}' not found", translation.LanguageId, translation.Name, typeNameForLog));
                    }
                }
            }
        }

        private class LocalDBImporter : ImportHelperBase
        {
            public LocalDBImporter(SQLiteTagsDB tagsDB, DbConnection connection, DbTransaction transaction, TextWriter logStream)
                : base(tagsDB, connection, transaction, logStream)
            {
            }

            protected override bool AllowEmptyPathInserts
            {
                get { return false; }
            }

            protected override void ImportFileTags(List<TagsForFileExchange> tags, String user)
            {
                if (tags == null)
                    return;

                String tagsQuery = String.Format("SELECT DISTINCT {0} FROM {1} WHERE {2}=@FileId", Schema.TAG_COLUMN, Schema.FILETAGS_TABLE, Schema.FILE_COLUMN);
                String userQuery = String.Format("SELECT {0} FROM {1} WHERE {2}=@TagId AND {3}=@FileId", Schema.USER_COLUMN, Schema.FILETAGS_TABLE, Schema.TAG_COLUMN, Schema.FILE_COLUMN);
                String removedQuery = String.Format("SELECT DISTINCT {0}, {3} FROM {1} WHERE {2}=@FileId", Schema.TAG_COLUMN, Schema.REMOVEDTAGS_TABLE, Schema.FILE_COLUMN, Schema.USER_COLUMN);
                String insertString = String.Format("INSERT INTO {0} ({1}, {2}, {3}, {4}) VALUES (@NewId, @FileId, @TagId, @User)",
                    Schema.FILETAGS_TABLE, Schema.ID_COLUMN, Schema.FILE_COLUMN, Schema.TAG_COLUMN, Schema.USER_COLUMN);
                String removeRemovedString = String.Format("DELETE FROM {0} WHERE {1}=@FileId AND {2}=@TagId", Schema.REMOVEDTAGS_TABLE, Schema.FILE_COLUMN, Schema.TAG_COLUMN);
                String updateString = String.Format("UPDATE {0} SET {1}=@User WHERE {2}=@TagId AND {3}=@FileId", Schema.FILETAGS_TABLE, Schema.USER_COLUMN, Schema.TAG_COLUMN, Schema.FILE_COLUMN);

                using (DbCommand tagsQueryCommand = DbUtils.CreateDbCommand(tagsQuery, m_Connection, m_Transaction),
                       userQueryCommand = DbUtils.CreateDbCommand(userQuery, m_Connection, m_Transaction),
                       removedQueryCommand = DbUtils.CreateDbCommand(removedQuery, m_Connection, m_Transaction),
                       removeRemovedCommand = DbUtils.CreateDbCommand(removeRemovedString, m_Connection, m_Transaction),
                       updateCommand = DbUtils.CreateDbCommand(updateString, m_Connection, m_Transaction),
                       insertCommand = DbUtils.CreateDbCommand(insertString, m_Connection, m_Transaction))
                {
                    DbParameter queryTagsFileParam = tagsQueryCommand.AddParameter("@FileId", System.Data.DbType.Int64);

                    DbParameter queryUserFileParam = userQueryCommand.AddParameter("@FileId", System.Data.DbType.Int64);
                    DbParameter queryUserTagParam = userQueryCommand.AddParameter("@TagId", System.Data.DbType.Int64);

                    DbParameter queryRemovedFileParam = removedQueryCommand.AddParameter("@FileId", System.Data.DbType.Int64);

                    insertCommand.AddParameterWithValue("@NewId", DBNull.Value);
                    DbParameter fileInsertParam = insertCommand.AddParameter("@FileId", System.Data.DbType.Int64);
                    DbParameter tagInsertParam = insertCommand.AddParameter("@TagId", System.Data.DbType.Int64);
                    insertCommand.AddParameterWithValue("@User", user);

                    DbParameter removeRemovedFileParam = removeRemovedCommand.AddParameter("@FileId", System.Data.DbType.Int64);
                    DbParameter tagidParam3 = removeRemovedCommand.AddParameter("@TagId", System.Data.DbType.Int64);

                    updateCommand.AddParameterWithValue("@User", user);
                    DbParameter updateFileParam = updateCommand.AddParameter("@FileId", System.Data.DbType.Int64);
                    DbParameter updateTagParam = updateCommand.AddParameter("@TagId", System.Data.DbType.Int64);

                    foreach (TagsForFileExchange fileTags in tags)
                    {
                        if (m_FilesMap.ContainsKey(fileTags.FileId))
                        {
                            long fileId = m_FilesMap[fileTags.FileId];
                            // find tags to add
                            HashSet<long> importedTags = new HashSet<long>();
                            foreach (long tagId in fileTags.TagIds)
                            {
                                if (m_TagsMap.ContainsKey(tagId))
                                {
                                    importedTags.Add(m_TagsMap[tagId]);
                                }
                                else
                                {
                                    m_LogStream.WriteLine(String.Format("WARNING: Unknown tag {0} for file {1}; ignoring assignment", tagId, fileTags.FileId));
                                }
                            }
                            // find existing tags --> those don't need to be added
                            queryTagsFileParam.Value = fileId;
                            using (DbDataReader reader = tagsQueryCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    long tagId = reader.GetInt64(0);
                                    if (importedTags.Contains(tagId))
                                    {
                                        m_LogStream.WriteLine(String.Format("Tag {0} already assigned to file {1}", tagId, fileId));
                                        importedTags.Remove(tagId);

                                        // check whether the existing tag was set from the global DB and is now set by a file
                                        // if so, change the user
                                        if (user != Schema.GLOBAL_DB_USER && !String.IsNullOrEmpty(user))
                                        {
                                            queryUserFileParam.Value = fileId;
                                            queryUserTagParam.Value = tagId;
                                            Object currentUser = userQueryCommand.ExecuteScalar();
                                            if (currentUser != null && currentUser != DBNull.Value && ((String)currentUser == Schema.GLOBAL_DB_USER))
                                            {
                                                updateFileParam.Value = fileId;
                                                updateTagParam.Value = tagId;
                                                updateCommand.ExecuteNonQuery();
                                                m_LogStream.WriteLine("Updated user of assignment from global db to file");
                                            }
                                        }
                                    }
                                }
                            }
                            // find tags removed by user himself --> those must not be added
                            queryRemovedFileParam.Value = fileId;
                            using (DbDataReader reader = removedQueryCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    long tagId = reader.GetInt64(0);
                                    String currentUser = reader.GetString(1);
                                    if (importedTags.Contains(tagId) && currentUser != Schema.GLOBAL_DB_USER)
                                    {
                                        m_LogStream.WriteLine(String.Format("Tag {0} was removed from file {1} and will not be assigned to it", tagId, fileId));
                                        importedTags.Remove(tagId);
                                    }
                                    else if (importedTags.Contains(tagId))
                                    {
                                        // remove from removedtags table
                                        m_LogStream.WriteLine(String.Format("Tag {0} was removed from file {1} by global db and will now be reassigned", tagId, fileId));
                                        removeRemovedFileParam.Value = fileId;
                                        tagidParam3.Value = tagId;
                                        removeRemovedCommand.ExecuteNonQuery();
                                    }
                                }
                            }
                            // insert remaining tags
                            if (importedTags.Count > 0)
                            {
                                fileInsertParam.Value = fileId;
                                foreach (Int64 tagId in importedTags)
                                {
                                    tagInsertParam.Value = tagId;
                                    insertCommand.ExecuteNonQuery();
                                    m_LogStream.WriteLine(String.Format("Assigned tag {0} to file {1}", tagId, fileId));
                                }
                            }
                        }
                        else
                        {
                            m_LogStream.WriteLine(String.Format("WARNING: Unknown file {0} in tags; ignoring assignment", fileTags.FileId));
                        }
                    }
                }
            }

            protected override void ImportRemovedTags(List<TagsForFileExchange> tags, String user)
            {
                if (tags == null || tags.Count == 0)
                    return;

                String tagsQuery = String.Format("SELECT DISTINCT {0} FROM {1} WHERE {2}=@FileId", Schema.TAG_COLUMN, Schema.REMOVEDTAGS_TABLE, Schema.FILE_COLUMN);
                String userQuery = String.Format("SELECT {0} FROM {1} WHERE {2}=@TagId AND {3}=@FileId", Schema.USER_COLUMN, Schema.REMOVEDTAGS_TABLE, Schema.TAG_COLUMN, Schema.FILE_COLUMN);
                String assignedQuery = String.Format("SELECT DISTINCT {0}, {3} FROM {1} WHERE {2}=@FileId", Schema.TAG_COLUMN, Schema.FILETAGS_TABLE, Schema.FILE_COLUMN, Schema.USER_COLUMN);
                String insertString = String.Format("INSERT INTO {0} ({1}, {2}, {3}, {4}) VALUES (@NewId, @FileId, @TagId, @User)",
                    Schema.REMOVEDTAGS_TABLE, Schema.ID_COLUMN, Schema.FILE_COLUMN, Schema.TAG_COLUMN, Schema.USER_COLUMN);
                String removeAssignedString = String.Format("DELETE FROM {0} WHERE {1}=@FileId AND {2}=@TagId", Schema.FILETAGS_TABLE, Schema.FILE_COLUMN, Schema.TAG_COLUMN);
                String updateString = String.Format("UPDATE {0} SET {1}=@User WHERE {2}=@TagId AND {3}=@FileId", Schema.REMOVEDTAGS_TABLE, Schema.USER_COLUMN, Schema.TAG_COLUMN, Schema.FILE_COLUMN);

                using (DbCommand tagsQueryCommand = DbUtils.CreateDbCommand(tagsQuery, m_Connection, m_Transaction),
                       userQueryCommand = DbUtils.CreateDbCommand(userQuery, m_Connection, m_Transaction),
                       assignedQueryCommand = DbUtils.CreateDbCommand(assignedQuery, m_Connection, m_Transaction),
                       removeAssignedCommand = DbUtils.CreateDbCommand(removeAssignedString, m_Connection, m_Transaction),
                       updateCommand = DbUtils.CreateDbCommand(updateString, m_Connection, m_Transaction),
                       insertCommand = DbUtils.CreateDbCommand(insertString, m_Connection, m_Transaction))
                {
                    DbParameter queryTagsFileParam = tagsQueryCommand.AddParameter("@FileId", System.Data.DbType.Int64);

                    DbParameter queryUserFileParam = userQueryCommand.AddParameter("@FileId", System.Data.DbType.Int64);
                    DbParameter queryUserTagParam = userQueryCommand.AddParameter("@TagId", System.Data.DbType.Int64);

                    DbParameter queryAssignedFileParam = assignedQueryCommand.AddParameter("@FileId", System.Data.DbType.Int64);

                    insertCommand.AddParameterWithValue("@NewId", DBNull.Value);
                    DbParameter fileInsertParam = insertCommand.AddParameter("@FileId", System.Data.DbType.Int64);
                    DbParameter tagInsertParam = insertCommand.AddParameter("@TagId", System.Data.DbType.Int64);
                    insertCommand.AddParameterWithValue("@User", user);

                    DbParameter removeAssignedFileParam = removeAssignedCommand.AddParameter("@FileId", System.Data.DbType.Int64);
                    DbParameter removeAssignedTagParam = removeAssignedCommand.AddParameter("@TagId", System.Data.DbType.Int64);

                    updateCommand.AddParameterWithValue("@User", user);
                    DbParameter updateFileParam = updateCommand.AddParameter("@FileId", System.Data.DbType.Int64);
                    DbParameter updateTagParam = updateCommand.AddParameter("@TagId", System.Data.DbType.Int64);

                    foreach (TagsForFileExchange fileTags in tags)
                    {
                        if (m_FilesMap.ContainsKey(fileTags.FileId))
                        {
                            long fileId = m_FilesMap[fileTags.FileId];
                            // find tags to add
                            HashSet<long> importedTags = new HashSet<long>();
                            foreach (long tagId in fileTags.TagIds)
                            {
                                if (m_TagsMap.ContainsKey(tagId))
                                {
                                    importedTags.Add(m_TagsMap[tagId]);
                                }
                                else
                                {
                                    m_LogStream.WriteLine(String.Format("WARNING: Unknown tag {0} for file {1}; ignoring removal", tagId, fileTags.FileId));
                                }
                            }
                            // find existing tags --> those don't need to be added to the removed-table
                            queryTagsFileParam.Value = fileId;
                            using (DbDataReader reader = tagsQueryCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    long tagId = reader.GetInt64(0);
                                    if (importedTags.Contains(tagId))
                                    {
                                        m_LogStream.WriteLine(String.Format("Tag {0} already removed from file {1}", tagId, fileId));
                                        importedTags.Remove(tagId);

                                        // check whether the existing tag was set from the global DB and is now set by a file
                                        // if so, change the user
                                        if (user != Schema.GLOBAL_DB_USER && !String.IsNullOrEmpty(user))
                                        {
                                            queryUserFileParam.Value = fileId;
                                            queryUserTagParam.Value = tagId;
                                            Object currentUser = userQueryCommand.ExecuteScalar();
                                            if (currentUser != null && currentUser != DBNull.Value && ((String)currentUser == Schema.GLOBAL_DB_USER))
                                            {
                                                updateFileParam.Value = fileId;
                                                updateTagParam.Value = tagId;
                                                updateCommand.ExecuteNonQuery();
                                                m_LogStream.WriteLine("Updated user of assignment from global db to file");
                                            }
                                        }
                                    }
                                }
                            }
                            // find tags assigned by user himself --> those must not be removed
                            queryAssignedFileParam.Value = fileId;
                            using (DbDataReader reader = assignedQueryCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    long tagId = reader.GetInt64(0);
                                    String currentUser = reader.GetStringOrEmpty(1);
                                    if (importedTags.Contains(tagId) && currentUser != Schema.GLOBAL_DB_USER)
                                    {
                                        m_LogStream.WriteLine(String.Format("Tag {0} was assigned to file {1} and will not be removed from it", tagId, fileId));
                                        importedTags.Remove(tagId);
                                    }
                                    else if (importedTags.Contains(tagId))
                                    {
                                        // remove from filetags table: was assigned by global DB, but removed by this import
                                        m_LogStream.WriteLine(String.Format("Tag {0} was assigned to file {1} by global db and will now be removed", tagId, fileId));
                                        removeAssignedFileParam.Value = fileId;
                                        removeAssignedTagParam.Value = tagId;
                                        removeAssignedCommand.ExecuteNonQuery();
                                    }
                                }
                            }
                            // insert remaining tags
                            if (importedTags.Count > 0)
                            {
                                fileInsertParam.Value = fileId;
                                foreach (Int64 tagId in importedTags)
                                {
                                    tagInsertParam.Value = tagId;
                                    insertCommand.ExecuteNonQuery();
                                    m_LogStream.WriteLine(String.Format("Removed tag {0} from file {1}", tagId, fileId));
                                }
                            }
                        }
                        else
                        {
                            m_LogStream.WriteLine(String.Format("WARNING: Unknown file {0} in tags; ignoring assignment", fileTags.FileId));
                        }
                    }
                }
            }
        }

        class GlobalDBImporter : ImportHelperBase
        {
            public GlobalDBImporter(SQLiteTagsDB tagsDB, DbConnection connection, DbTransaction transaction, TextWriter logStream)
                : base(tagsDB, connection, transaction, logStream)
            {
            }

            protected override bool AllowEmptyPathInserts
            {
                get { return true; }
            }

            protected override void ImportFileTags(List<TagsForFileExchange> tags, string user)
            {
                if (tags == null || tags.Count == 0)
                    return;

                String queryExisting = String.Format("SELECT {0} FROM {1} WHERE {2}=@FileId AND {3}=@TagId AND {4}=@User",
                    Schema.ID_COLUMN, Schema.FILETAGS_TABLE, Schema.FILE_COLUMN, Schema.TAG_COLUMN, Schema.USER_COLUMN);
                String removeRemoved = String.Format("DELETE FROM {0} WHERE {1}=@FileId AND {2}=@TagId AND {3}=@User",
                    Schema.REMOVEDTAGS_TABLE, Schema.FILE_COLUMN, Schema.TAG_COLUMN, Schema.USER_COLUMN);
                String insertNew = String.Format("INSERT INTO {0} ({1}, {2}, {3}, {4}) VALUES (@Id, @FileId, @TagId, @User)",
                    Schema.FILETAGS_TABLE, Schema.ID_COLUMN, Schema.FILE_COLUMN, Schema.TAG_COLUMN, Schema.USER_COLUMN);
                using (DbCommand queryExistingCmd = DbUtils.CreateDbCommand(queryExisting, m_Connection, m_Transaction),
                       removeRemovedCmd = DbUtils.CreateDbCommand(removeRemoved, m_Connection, m_Transaction),
                       insertNewCmd = DbUtils.CreateDbCommand(insertNew, m_Connection, m_Transaction))
                {
                    DbParameter queryFileParam = queryExistingCmd.AddParameter("@FileId", System.Data.DbType.Int64);
                    DbParameter queryTagParam = queryExistingCmd.AddParameter("@TagId", System.Data.DbType.Int64);
                    queryExistingCmd.AddParameterWithValue("@User", user);

                    DbParameter removeFileParam = removeRemovedCmd.AddParameter("@FileId", System.Data.DbType.Int64);
                    DbParameter removeTagParam = removeRemovedCmd.AddParameter("@TagId", System.Data.DbType.Int64);
                    removeRemovedCmd.AddParameterWithValue("@User", user);

                    insertNewCmd.AddParameterWithValue("@Id", System.DBNull.Value);
                    DbParameter insertFileParam = insertNewCmd.AddParameter("@FileId", System.Data.DbType.Int64);
                    DbParameter insertTagParam = insertNewCmd.AddParameter("@TagId", System.Data.DbType.Int64);
                    insertNewCmd.AddParameterWithValue("@User", user);

                    foreach (TagsForFileExchange tagsForFile in tags)
                    {
                        if (tagsForFile.TagIds == null || tagsForFile.TagIds.Count == 0)
                        {
                            m_LogStream.Write(String.Format("No tags to add for file {0}", tagsForFile.FileId));
                            continue;
                        }

                        long fileId = -1;
                        if (m_FilesMap.ContainsKey(tagsForFile.FileId))
                        {
                            fileId = m_FilesMap[tagsForFile.FileId];
                        }
                        else
                        {
                            m_LogStream.WriteLine(String.Format("WARNING: Unknown file {0} in tags; ignoring assignments", tagsForFile.FileId));
                            continue;
                        }
                        queryFileParam.Value = fileId;
                        removeFileParam.Value = fileId;
                        insertFileParam.Value = fileId;

                        foreach (long tagId in tagsForFile.TagIds)
                        {
                            long realId = -1;
                            if (m_TagsMap.ContainsKey(tagId))
                            {
                                realId = m_TagsMap[tagId];
                            }
                            else
                            {
                                m_LogStream.WriteLine(String.Format("WARNING: Unknown tag {0} for file {1}; ignoring assignment", tagId, tagsForFile.FileId));
                                continue;
                            }
                            queryTagParam.Value = realId;
                            if (queryExistingCmd.ExecuteScalar() != null)
                            {
                                m_LogStream.WriteLine(String.Format("Tag {0} already assigned to file {1} by user {2}; ignoring assignment", tagId, tagsForFile.FileId, user));
                                continue;
                            }
                            insertTagParam.Value = realId;
                            insertNewCmd.ExecuteNonQuery();
                            m_LogStream.WriteLine(String.Format("Tag {0} assigned to file {1} for user {2}", tagId, tagsForFile.FileId, user));
                            removeTagParam.Value = realId;
                            if (removeRemovedCmd.ExecuteNonQuery() > 0)
                            {
                                m_LogStream.WriteLine(String.Format("Tag {0} no longer removed from file {1} for user {2}", tagId, tagsForFile.FileId, user));
                            }
                        }
                    }
                }
            }

            protected override void ImportRemovedTags(List<TagsForFileExchange> tags, string user)
            {
                if (tags == null || tags.Count == 0)
                    return;

                String queryExisting = String.Format("SELECT {0} FROM {1} WHERE {2}=@FileId AND {3}=@TagId AND {4}=@User",
                    Schema.ID_COLUMN, Schema.REMOVEDTAGS_TABLE, Schema.FILE_COLUMN, Schema.TAG_COLUMN, Schema.USER_COLUMN);
                String removeAssigned = String.Format("DELETE FROM {0} WHERE {1}=@FileId AND {2}=@TagId AND {3}=@User",
                    Schema.FILETAGS_TABLE, Schema.FILE_COLUMN, Schema.TAG_COLUMN, Schema.USER_COLUMN);
                String insertNew = String.Format("INSERT INTO {0} ({1}, {2}, {3}, {4}) VALUES (@Id, @FileId, @TagId, @User)",
                    Schema.REMOVEDTAGS_TABLE, Schema.ID_COLUMN, Schema.FILE_COLUMN, Schema.TAG_COLUMN, Schema.USER_COLUMN);
                using (DbCommand queryExistingCmd = DbUtils.CreateDbCommand(queryExisting, m_Connection, m_Transaction),
                       removeAssignedCmd = DbUtils.CreateDbCommand(removeAssigned, m_Connection, m_Transaction),
                       insertNewCmd = DbUtils.CreateDbCommand(insertNew, m_Connection, m_Transaction))
                {
                    DbParameter queryFileParam = queryExistingCmd.AddParameter("@FileId", System.Data.DbType.Int64);
                    DbParameter queryTagParam = queryExistingCmd.AddParameter("@TagId", System.Data.DbType.Int64);
                    queryExistingCmd.AddParameterWithValue("@User", user);

                    DbParameter removeFileParam = removeAssignedCmd.AddParameter("@FileId", System.Data.DbType.Int64);
                    DbParameter removeTagParam = removeAssignedCmd.AddParameter("@TagId", System.Data.DbType.Int64);
                    removeAssignedCmd.AddParameterWithValue("@User", user);

                    insertNewCmd.AddParameterWithValue("@Id", System.DBNull.Value);
                    DbParameter insertFileParam = insertNewCmd.AddParameter("@FileId", System.Data.DbType.Int64);
                    DbParameter insertTagParam = insertNewCmd.AddParameter("@TagId", System.Data.DbType.Int64);
                    insertNewCmd.AddParameterWithValue("@User", user);

                    foreach (TagsForFileExchange tagsForFile in tags)
                    {
                        if (tagsForFile.TagIds == null || tagsForFile.TagIds.Count == 0)
                        {
                            m_LogStream.Write(String.Format("No tags to remove for file {0}", tagsForFile.FileId));
                            continue;
                        }

                        long fileId = -1;
                        if (m_FilesMap.ContainsKey(tagsForFile.FileId))
                        {
                            fileId = m_FilesMap[tagsForFile.FileId];
                        }
                        else
                        {
                            m_LogStream.WriteLine(String.Format("WARNING: Unknown file {0} in tags; ignoring removals", tagsForFile.FileId));
                            continue;
                        }
                        queryFileParam.Value = fileId;
                        removeFileParam.Value = fileId;
                        insertFileParam.Value = fileId;

                        foreach (long tagId in tagsForFile.TagIds)
                        {
                            long realId = -1;
                            if (m_TagsMap.ContainsKey(tagId))
                            {
                                realId = m_TagsMap[tagId];
                            }
                            else
                            {
                                m_LogStream.WriteLine(String.Format("WARNING: Unknown tag {0} for file {1}; ignoring removal", tagId, tagsForFile.FileId));
                                continue;
                            }
                            queryTagParam.Value = realId;
                            if (queryExistingCmd.ExecuteScalar() != null)
                            {
                                m_LogStream.WriteLine(String.Format("Tag {0} already removed from file {1} by user {2}; ignoring removal", tagId, tagsForFile.FileId, user));
                                continue;
                            }
                            insertTagParam.Value = realId;
                            insertNewCmd.ExecuteNonQuery();
                            m_LogStream.WriteLine(String.Format("Tag {0} removed from file {1} for user {2}", tagId, tagsForFile.FileId, user));
                            removeTagParam.Value = realId;
                            if (removeAssignedCmd.ExecuteNonQuery() > 0)
                            {
                                m_LogStream.WriteLine(String.Format("Tag {0} no longer assigned to file {1} by user {2}", tagId, tagsForFile.FileId, user));
                            }
                        }
                    }
                }
            }
        }

        #endregion

    }

    class FileFinder : IDisposable
    {
        private DbCommand queryByAcoustIdCmd;
        private DbCommand queryByArtistAndTitleCmd;
        private DbParameter acoustIdParam;
        private DbParameter artistParam;
        private DbParameter titleParam;

        private bool m_Disposed = false;

        public FileFinder(DbConnection connection, DbTransaction transaction)
        {
            String queryByAcoustId = String.Format("SELECT {0}, {3} FROM {1} WHERE {2}=@AcoustId", Schema.ID_COLUMN, Schema.FILES_TABLE, Schema.ACOUST_ID_COLUMN, Schema.PATH_COLUMN);
            String queryByArtistAndTitle = String.Format("SELECT {0}, {4} FROM {1} WHERE {2}=@Artist AND {3}=@Title",
                Schema.ID_COLUMN, Schema.FILES_TABLE, Schema.ARTIST_COLUMN, Schema.TITLE_COLUMN, Schema.PATH_COLUMN);
            queryByAcoustIdCmd = DbUtils.CreateDbCommand(queryByAcoustId, connection, transaction);
            queryByArtistAndTitleCmd = DbUtils.CreateDbCommand(queryByArtistAndTitle, connection, transaction);
            acoustIdParam = queryByAcoustIdCmd.AddParameter("@AcoustId", System.Data.DbType.String);
            artistParam = queryByArtistAndTitleCmd.AddParameter("@Artist", System.Data.DbType.String);
            titleParam = queryByArtistAndTitleCmd.AddParameter("@Title", System.Data.DbType.String);
        }

        public void Dispose()
        {
            queryByAcoustIdCmd.Dispose();
            queryByArtistAndTitleCmd.Dispose();
            m_Disposed = true;
        }

        public long FindFileByIdentification(FileIdentification file, IList<String> filesToMatch, TextWriter logStream)
        {
            if (m_Disposed)
                throw new ObjectDisposedException("FileFinder");
            long id = -1;
            // try to find by acoust Id
            if (!String.IsNullOrEmpty(file.AcoustId))
            {
                acoustIdParam.Value = file.AcoustId;
                using (DbDataReader reader = queryByAcoustIdCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (filesToMatch == null)
                        {
                            id = reader.GetInt64(0);
                            break;
                        }
                        else if (filesToMatch.Contains(reader.GetString(1)))
                        {
                            id = reader.GetInt64(0);
                            break;
                        }
                        else if (logStream != null)
                        {
                            logStream.WriteLine(String.Format("Info: found existing file {0} matching AcoustID - duplicate?", reader.GetString(1)));
                        }
                    }
                }
            }
            // try to find by artist / title
            if (id == -1 && !String.IsNullOrEmpty(file.Artist) && !String.IsNullOrEmpty(file.Title))
            {
                artistParam.Value = file.Artist;
                titleParam.Value = file.Title;
                using (DbDataReader reader = queryByArtistAndTitleCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (filesToMatch == null)
                        {
                            id = reader.GetInt64(0);
                            break;
                        }
                        else if (filesToMatch.Contains(reader.GetString(1)))
                        {
                            id = reader.GetInt64(0);
                            break;
                        }
                        else if (logStream != null)
                        {
                            logStream.WriteLine(String.Format("Info: found existing file {0} matching title and artist - duplicate?", reader.GetString(1)));
                        }
                    }
                }
            }
            return id;
        }
    }
}