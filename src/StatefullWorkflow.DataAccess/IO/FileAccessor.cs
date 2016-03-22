﻿using System;
using System.Collections.Generic;

//using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using StatefullWorkflow.Entities;
using StatefullWorkflow.DataAccess.Exceptions;

//using PCLStorage;
using PCLStorage;
using System.Threading.Tasks;

namespace StatefullWorkflow.DataAccess.IO
{
    public class FileAccessor : IFileAccessor
    {
        public IPluralizer Pluralizer { get; set; }

        public FileAccessor()
        {
            Pluralizer = new InflectorPluralizer();//DataEntityPluralizer();//PluralizationService.CreateService(CultureInfo.CurrentCulture);
        }

        public async Task<bool>  FileExists<TEntity, Tid>(string folder) where TEntity : Entity<Tid> where Tid : struct
        {
            if (String.IsNullOrEmpty(folder))
            {
                throw new DataAccessException("A folder must be provided");
            }
            var className = typeof(TEntity).Name;

            // get hold of the file system
            IFolder rootFolder = FileSystem.Current.LocalStorage;

            // create a folder, if one does not exist already
            var fileFullName = GetFileFullName(folder, className);
            var exists = await rootFolder.CheckExistsAsync(fileFullName);
            return exists == ExistenceCheckResult.FileExists;
        }

        public async Task<string> ReadFile<TEntity, Tid>(string folder) where TEntity : Entity<Tid> where Tid : struct
        {
            if (String.IsNullOrEmpty(folder))
            {
                throw new DataAccessException("A folder must be provided");
            }
            
            var className = typeof(TEntity).Name;
            var name = GetFileName(className);

            IFolder directory = await FileSystem.Current.GetFolderFromPathAsync(folder);
            if (directory == null)
            {
                throw new DataAccessException("File not found");
            }

            IFile file = await directory.GetFileAsync(name);

            if (file == null)
            {
                throw new DataAccessException("File not found");
            }

            return await file.ReadAllTextAsync();
        }

        public async Task<bool> SaveToFile<TEntity, Tid>(string folder, string contents) where TEntity : Entity<Tid> where Tid : struct
        {
            try
            {
                var className = typeof(TEntity).Name;
                var name = GetFileName(className);
                            
                IFolder rootFolder = FileSystem.Current.LocalStorage;
                IFolder directory = await rootFolder.CreateFolderAsync(folder, CreationCollisionOption.OpenIfExists);

                IFile file = await directory.CreateFileAsync(name, CreationCollisionOption.OpenIfExists);
                await file.WriteAllTextAsync(contents);
            }
            catch (Exception ex)
            {
                //NLog.LogManager.GetLogger("Standard").Error(ex, ex.Message);
                throw;
            }
            return true;
        }

        public string GetFileName(string className)
        {
            var name = Pluralizer.Pluralize(className);
            return name + ".json";
        }

        public string GetFileFullName(string folder, string className)
        {
            var name = Pluralizer.Pluralize(className);
            if (folder.EndsWith("/") || folder.EndsWith(@"\"))
                return folder + name + ".json";
            else if (folder.Contains("/"))
                return folder + "/" + name + ".json";
            return folder + @"\" + name + ".json";
        }

        public string JoinPaths(string partA, string partB)
        {
            if (partA.EndsWith("/") || partA.EndsWith(@"\"))
                return partA + partB;
            else if (partA.Contains("/"))
            {
                if (partB.StartsWith("/"))
                    return partA + partB;
                return partA + "/" + partB;
            }
            if (partB.StartsWith(@"\"))
                return partA + partB;
            return partA + @"\" + partB;
        }
    }
}
