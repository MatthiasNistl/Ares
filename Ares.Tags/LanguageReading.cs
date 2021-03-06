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
using System.Text;

using System.Data.Common;

namespace Ares.Tags
{

    class LanguageReading : ITagsDBReadByLanguage, IConnectionClient
    {
        private int m_LanguageId;
        private DbConnection m_Connection;

        internal LanguageReading(int languageId, DbConnection connection)
        {
            m_LanguageId = languageId;
            m_Connection = connection;
        }

        public void ConnectionChanged(DbConnection connection)
        {
            m_Connection = connection;
        }

        public IList<CategoryForLanguage> GetAllCategories()
        {
            if (m_Connection == null)
            {
                throw new TagsDbException("No Connection to DB file!");
            }
            try
            {
                return DoGetAllCategories();
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

        public IList<TagForLanguage> GetAllTags(int categoryId)
        {
            if (m_Connection == null)
            {
                throw new TagsDbException("No Connection to DB file!");
            }
            try
            {
                return DoGetAllTags(categoryId);
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

        public IList<TagInfoForLanguage> GetTagsForFile(string relativePath)
        {
            if (m_Connection == null)
            {
                throw new TagsDbException("No Connection to DB file!");
            }
            try
            {
                return DoGetTagsForFile(relativePath);
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

        public IList<TagInfoForLanguage> GetTagsForFile(Int64 id)
        {
            if (m_Connection == null)
            {
                throw new TagsDbException("No Connection to DB file!");
            }
            try
            {
                return DoGetTagsForFile(id);
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

        public IList<TagInfoForLanguage> GetAllTags()
        {
            if (m_Connection == null)
            {
                throw new TagsDbException("No Connection to DB file!");
            }
            try
            {
                return DoGetAllTags();
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

        public IList<LanguageForLanguage> GetAllLanguages()
        {
            if (m_Connection == null)
            {
                throw new TagsDbException("No Connection to DB file!");
            }
            try
            {
                return DoGetAllLanguages();
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

        public IList<TagInfoForLanguage> GetTagInfos(ICollection<int> tagIds)
        {
            if (m_Connection == null)
            {
                throw new TagsDbException("No Connection to DB file!");
            }
            try
            {
                return DoGetTagInfos(tagIds);
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

        public long FindTag(String category, String tagName)
        {
            if (m_Connection == null)
            {
                throw new TagsDbException("No Connection to DB file!");
            }
            try
            {
                return DoFindTag(category, tagName);
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

        private IList<CategoryForLanguage> DoGetAllCategories()
        {
            String query = String.Format("SELECT {0}.{1}, {2}.{3} FROM {0}, {2} WHERE {0}.{1}={2}.{4} AND {2}.{5}=@LangId ORDER BY {2}.{3}",
                Schema.CATEGORIES_TABLE, Schema.ID_COLUMN, Schema.CATEGORYNAMES_TABLE, Schema.NAME_COLUMN, Schema.CATEGORY_COLUMN, Schema.LANGUAGE_COLUMN);
            using (DbCommand command = DbUtils.CreateDbCommand(query, m_Connection))
            {
                command.AddParameterWithValue("@LangId", m_LanguageId);
                List<CategoryForLanguage> result = new List<CategoryForLanguage>();
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CategoryForLanguage() { Id = (int)reader.GetInt64(0), Name = reader.GetString(1) });
                    }
                }
                return result;
            }
        }

        private IList<TagForLanguage> DoGetAllTags(int categoryId)
        {
            String query = String.Format("SELECT {0}.{1}, {2}.{3} FROM {0}, {2} WHERE {0}.{1}={2}.{4} AND {2}.{5}=@LangId AND {0}.{6}=@CatId ORDER BY {2}.{3}",
                Schema.TAGS_TABLE, Schema.ID_COLUMN, Schema.TAGNAMES_TABLE, Schema.NAME_COLUMN, Schema.TAG_COLUMN, Schema.LANGUAGE_COLUMN, Schema.CATEGORY_COLUMN);
            using (DbCommand command = DbUtils.CreateDbCommand(query, m_Connection))
            {
                command.AddParameterWithValue("@LangId", m_LanguageId);
                command.AddParameterWithValue("@CatId", categoryId);
                List<TagForLanguage> result = new List<TagForLanguage>();
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new TagForLanguage() { Id = (int)reader.GetInt64(0), Name = reader.GetString(1) });
                    }
                }
                return result;                
            }
        }

        private IList<TagInfoForLanguage> DoGetTagsForFile(string relativePath)
        {
            // 0: Tags; 2: TagNames; 4: CategoryNames; 7: Categories; 8: Files; 9: FileTags
            // 1: Id; 3: Name; 5: CategoryName; 6: CategoryId; 10: Tag; 11: File; 12: Language; 13: Path
            String query = String.Format("SELECT {0}.{1}, {2}.{3}, {4}.{5}, {0}.{6} FROM {0}, {2}, {4}, {7}, {8}, {9} "
                // joins
                + "WHERE {0}.{1}={2}.{10} " // Tag with TagNames
                + "AND {0}.{6}={7}.{1} " // Tag with Category
                + "AND {7}.{1}={4}.{6} " // Category with CategoryNames
                + "AND {8}.{1}={9}.{11} " // File with FileTags
                + "AND {0}.{1}={9}.{10} " // Tag with FileTags
                // conditions
                + "AND {2}.{12}=@LangId1 " // Tag Language
                + "AND {4}.{12}=@LangId2 " // Category Language
                + "AND {8}.{13}=@Path "    // File Path
                // sorting
                + "ORDER BY {4}.{5}, {2}.{3} ",
                // 0 - 4
                Schema.TAGS_TABLE, Schema.ID_COLUMN, Schema.TAGNAMES_TABLE, Schema.NAME_COLUMN, Schema.CATEGORYNAMES_TABLE,
                // 5 - 9
                Schema.NAME_COLUMN, Schema.CATEGORY_COLUMN, Schema.CATEGORIES_TABLE, Schema.FILES_TABLE, Schema.FILETAGS_TABLE,
                // 10 - 13
                Schema.TAG_COLUMN, Schema.FILE_COLUMN, Schema.LANGUAGE_COLUMN, Schema.PATH_COLUMN);
            using (DbCommand command = DbUtils.CreateDbCommand(query, m_Connection))
            {
                command.AddParameterWithValue("@LangId1", m_LanguageId);
                command.AddParameterWithValue("@LangId2", m_LanguageId);
                command.AddParameterWithValue("@Path", relativePath);
                List<TagInfoForLanguage> result = new List<TagInfoForLanguage>();
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new TagInfoForLanguage()
                        {
                            Id = (int)reader.GetInt64(0),
                            Name = reader.GetString(1),
                            Category = reader.GetString(2),
                            CategoryId = (int)reader.GetInt64(3)
                        });
                    }
                }
                return result;
            }
        }

        private IList<TagInfoForLanguage> DoGetTagsForFile(Int64 id)
        {
            // also check that the tag is assigned more often than it is removed (this query is for the global DB)
            String countAddsQuery = String.Format("SELECT count(*) FROM {0} WHERE {0}.{1}={2}.{3} AND {0}.{4}={5}.{6}",
                Schema.FILETAGS_TABLE, Schema.FILE_COLUMN, Schema.FILES_TABLE, Schema.ID_COLUMN, Schema.TAG_COLUMN, Schema.TAGS_TABLE, Schema.ID_COLUMN);
            String countRemovesQuery = String.Format("SELECT count(*) FROM {0} WHERE {0}.{1}={2}.{3} AND {0}.{4}={5}.{6}",
                Schema.REMOVEDTAGS_TABLE, Schema.FILE_COLUMN, Schema.FILES_TABLE, Schema.ID_COLUMN, Schema.TAG_COLUMN, Schema.TAGS_TABLE, Schema.ID_COLUMN);
            // 0: Tags; 2: TagNames; 4: CategoryNames; 7: Categories; 8: Files; 9: FileTags
            // 1: Id; 3: Name; 5: CategoryName; 6: CategoryId; 10: Tag; 11: File; 12: Language; 13: File ID
            // 14: count Adds; 15: count Removes
            String query = String.Format("SELECT DISTINCT {0}.{1}, {2}.{3}, {4}.{5}, {0}.{6} FROM {0}, {2}, {4}, {7}, {8}, {9} "
                // joins
                + "WHERE {0}.{1}={2}.{10} " // Tag with TagNames
                + "AND {0}.{6}={7}.{1} " // Tag with Category
                + "AND {7}.{1}={4}.{6} " // Category with CategoryNames
                + "AND {8}.{1}={9}.{11} " // File with FileTags
                + "AND {0}.{1}={9}.{10} " // Tag with FileTags
                // conditions
                + "AND {2}.{12}=@LangId1 " // Tag Language
                + "AND {4}.{12}=@LangId2 " // Category Language
                + "AND {8}.{13}=@FileId "  // File Id
                + "AND ({14}) > ({15}) "   // adds > removes
                // sorting
                + "ORDER BY {4}.{5}, {2}.{3} ",
                // 0 - 4
                Schema.TAGS_TABLE, Schema.ID_COLUMN, Schema.TAGNAMES_TABLE, Schema.NAME_COLUMN, Schema.CATEGORYNAMES_TABLE,
                // 5 - 9
                Schema.NAME_COLUMN, Schema.CATEGORY_COLUMN, Schema.CATEGORIES_TABLE, Schema.FILES_TABLE, Schema.FILETAGS_TABLE,
                // 10 - 13
                Schema.TAG_COLUMN, Schema.FILE_COLUMN, Schema.LANGUAGE_COLUMN, Schema.ID_COLUMN,
                // 14 - 15
                countAddsQuery, countRemovesQuery);
            using (DbCommand command = DbUtils.CreateDbCommand(query, m_Connection))
            {
                command.AddParameterWithValue("@LangId1", m_LanguageId);
                command.AddParameterWithValue("@LangId2", m_LanguageId);
                command.AddParameterWithValue("@FileId", id);
                List<TagInfoForLanguage> result = new List<TagInfoForLanguage>();
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new TagInfoForLanguage()
                        {
                            Id = (int)reader.GetInt64(0),
                            Name = reader.GetString(1),
                            Category = reader.GetString(2),
                            CategoryId = (int)reader.GetInt64(3)
                        });
                    }
                }
                return result;
            }
        }

        private IList<TagInfoForLanguage> DoGetAllTags()
        {
            // 0: Tags; 2: TagNames; 4: CategoryNames; 7: Categories
            // 1: Id; 3: Name; 5: CategoryName; 6: CategoryId; 8: Tag; 9: Language
            String query = String.Format("SELECT {0}.{1}, {2}.{3}, {4}.{5}, {0}.{6} FROM {0}, {2}, {4}, {7} "
                // joins
                + "WHERE {0}.{1}={2}.{8} AND {0}.{6}={7}.{1} AND {7}.{1}={4}.{6} "
                // conditions
                + "AND {2}.{9}=@LangId1 AND {4}.{9}=@LangId2 "
                // sorting
                + "ORDER BY {2}.{3}",
                // 0 - 4
                Schema.TAGS_TABLE, Schema.ID_COLUMN, Schema.TAGNAMES_TABLE, Schema.NAME_COLUMN, Schema.CATEGORYNAMES_TABLE,
                // 5 - 9
                Schema.NAME_COLUMN, Schema.CATEGORY_COLUMN, Schema.CATEGORIES_TABLE, Schema.TAG_COLUMN, Schema.LANGUAGE_COLUMN);
            using (DbCommand command = DbUtils.CreateDbCommand(query, m_Connection))
            {
                command.AddParameterWithValue("@LangId1", m_LanguageId);
                command.AddParameterWithValue("@LangId2", m_LanguageId);
                List<TagInfoForLanguage> result = new List<TagInfoForLanguage>();
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new TagInfoForLanguage()
                        {
                            Id = (int)reader.GetInt64(0),
                            Name = reader.GetString(1),
                            Category = reader.GetString(2),
                            CategoryId = (int)reader.GetInt64(3)
                        });
                    }
                }
                return result;
            }
        }

        private IList<LanguageForLanguage> DoGetAllLanguages()
        {
            String query = String.Format("SELECT {0}.{1}, {2}.{3}, {0}.{6} FROM {0}, {2} WHERE {0}.{1}={2}.{4} AND {2}.{5}=@LangId ORDER BY {2}.{3}",
                Schema.LANGUAGE_TABLE, Schema.ID_COLUMN, Schema.LANGUAGENAMES_TABLE, Schema.NAME_COLUMN, Schema.NAMED_LANGUAGE_COLUMN, Schema.LANGUAGE_OF_NAME_COLUMN, Schema.LC_COLUMN);
            using (DbCommand command = DbUtils.CreateDbCommand(query, m_Connection))
            {
                command.AddParameterWithValue("@LangId", m_LanguageId);
                List<LanguageForLanguage> result = new List<LanguageForLanguage>();
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new LanguageForLanguage() { Id = (int)reader.GetInt64(0), Name = reader.GetString(1), Code = reader.GetString(2) });
                    }
                }
                return result;
            }
        }

        private IList<TagInfoForLanguage> DoGetTagInfos(ICollection<int> tagIds)
        {
            if (tagIds.Count == 0)
            {
                return new List<TagInfoForLanguage>();
            }

            // 0: Tags; 2: TagNames; 4: Categories; 6: CategoryNames
            // 1: Id; 3: Name; 5: Id; 7: Name; 8: Tag; 9: Category; 10: Language
            String query = String.Format("SELECT {0}.{1}, {2}.{3}, {4}.{5}, {6}.{7} FROM {0}, {2}, {4}, {6} "
                // Joins
                + "WHERE {0}.{1}={2}.{8} AND {0}.{9}={4}.{1} AND {4}.{1}={6}.{9} "
                // Languages
                + "AND {2}.{10}=@LangId1 AND {6}.{10}=@LangId2 "
                // IDs
                + "AND {0}.{1} IN (",
                // 0 - 4
                Schema.TAGS_TABLE, Schema.ID_COLUMN, Schema.TAGNAMES_TABLE, Schema.NAME_COLUMN, Schema.CATEGORIES_TABLE,
                // 5 - 8
                Schema.ID_COLUMN, Schema.CATEGORYNAMES_TABLE, Schema.NAME_COLUMN, Schema.TAG_COLUMN,
                // 9 - 10
                Schema.CATEGORY_COLUMN, Schema.LANGUAGE_COLUMN);
            // add ids
            int i = 0;
            foreach (int id in tagIds)
            {
                query += id;
                if (i < tagIds.Count - 1)
                    query += ",";
                ++i;
            }
            query += String.Format(") ORDER BY {0}.{1}, {2}.{3}",
                Schema.CATEGORYNAMES_TABLE, Schema.NAME_COLUMN, Schema.TAGNAMES_TABLE, Schema.NAME_COLUMN);
            using (DbCommand command = DbUtils.CreateDbCommand(query, m_Connection))
            {
                command.AddParameterWithValue("@LangId1", m_LanguageId);
                command.AddParameterWithValue("@LangId2", m_LanguageId);
                List<TagInfoForLanguage> result = new List<TagInfoForLanguage>();
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new TagInfoForLanguage()
                        {
                            Id = (int)reader.GetInt64(0),
                            Name = reader.GetString(1),
                            CategoryId = (int)reader.GetInt64(2),
                            Category = reader.GetString(3)
                        });
                    }
                }
                return result;
            }
        }

        private long DoFindTag(String category, String tagName)
        {
            if (String.IsNullOrEmpty(tagName))
                return -1;
            if (String.IsNullOrEmpty(category))
            {
                String query = String.Format("SELECT {0} FROM {1} WHERE {2}=@TagName AND {3}=@LanguageId", Schema.TAG_COLUMN, Schema.TAGNAMES_TABLE, Schema.NAME_COLUMN, Schema.LANGUAGE_COLUMN);
                using (DbCommand command = DbUtils.CreateDbCommand(query, m_Connection))
                {
                    command.AddParameterWithValue("@TagName", tagName);
                    command.AddParameterWithValue("@LanguageId", m_LanguageId);
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetInt64(0);
                        }
                        else
                        {
                            return -1;
                        }
                    }
                }
            }
            else
            {
                String query = String.Format("SELECT {0}.{1} FROM {0}, {2}, {3}, {4} WHERE {0}.{5}={2}.{1} AND {2}.{6}={3}.{7} AND {3}.{8}=@Category AND {0}.{1}={4}.{9} AND {4}.{10}=@TagName AND {3}.{11}=@LangId1 AND {4}.{11}=@LangId2",
                    Schema.TAGS_TABLE, Schema.ID_COLUMN, Schema.CATEGORIES_TABLE, Schema.CATEGORYNAMES_TABLE, Schema.TAGNAMES_TABLE,
                    Schema.CATEGORY_COLUMN, Schema.ID_COLUMN, Schema.CATEGORY_COLUMN, Schema.NAME_COLUMN, Schema.TAG_COLUMN, Schema.NAME_COLUMN, Schema.LANGUAGE_COLUMN);
                using (DbCommand command = DbUtils.CreateDbCommand(query, m_Connection))
                {
                    command.AddParameterWithValue("@TagName", tagName);
                    command.AddParameterWithValue("@Category", category);
                    command.AddParameterWithValue("@LangId1", m_LanguageId);
                    command.AddParameterWithValue("@LangId2", m_LanguageId);
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetInt64(0);
                        }
                        else
                        {
                            return -1;
                        }
                    }
                }
            }
        }

    }
}