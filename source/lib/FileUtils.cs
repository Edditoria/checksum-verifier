﻿// <copyright file="FileUtils.cs" company="Nic Jansma">
// Copyright (c) 2014 Nic Jansma All Right Reserved
// </copyright>
// <author>Nic Jansma</author>
namespace ChecksumVerifier
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// File utilities
    /// </summary>
    internal static class FileUtils
    {
        /// <summary>
        /// Gets all files, ignoring UnauthorizedAccessException's
        /// </summary>
        /// <param name="basePath">Base path</param>
        /// <param name="exclude">Excluding pattern</param>
        /// <param name="match">Match pattern</param>
        /// <param name="recurse">Recursive into subdirectories</param>
        /// <returns>List of files found</returns>
        public static List<string> GetFilesRecursive(string basePath, string exclude, string match, bool recurse)
        {
            List<string> newFiles = new List<string>();

            try
            {
                // add all files in this path
                newFiles.AddRange(GetFilesInDirectory(basePath, exclude, match));

                if (recurse)
                {
                    foreach (string subDir in Directory.GetDirectories(basePath))
                    {
                        // add all files in subdirs
                        string newPath = Path.Combine(basePath, subDir);                    
                        newFiles.AddRange(GetFilesRecursive(newPath, exclude, match, recurse));
                    }    
                }
            }
            catch (UnauthorizedAccessException)
            {
                // ignore any paths we can't get into
            }
            
            return newFiles;
        }

        /// <summary>
        /// Gets all files in the specified directory
        /// </summary>
        /// <param name="dir">Directory to scan</param>
        /// <param name="exclude">Exclude pattern</param>
        /// <param name="match">Match pattern</param>
        /// <returns>List of files in the specified directory</returns>
        public static string[] GetFilesInDirectory(string dir, string exclude, string match)
        {
            if (!Directory.Exists(dir))
            {
                return new string[0];
            }

            try
            {
                // gets all files in this directory
                string[] files = Directory.GetFiles(dir, match, SearchOption.TopDirectoryOnly);

                // now we have to apply our custom exlcusion patterns
                if (!String.IsNullOrEmpty(exclude))
                {
                    //
                    // Create a RegEx from a simple glob
                    //
                    Regex mask = new Regex(exclude.Replace(".", "[.]").Replace("*", ".*").Replace("?", "."));

                    List<string> filesToAdd = new List<string>();
                    foreach (string file in files)
                    {
                        // exclude any files that match our exclude pattern
                        if (!mask.IsMatch(file))
                        {
                            filesToAdd.Add(file);
                        }
                    }

                    return filesToAdd.ToArray();
                }
                else 
                {
                    return files;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // ignore any paths we can't get into
            }

            return new string[0];
        }

        /// <summary>
        /// Gets a file's MD5 checksum
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="checksumType">Checksum type</param>
        /// <returns>File's checksum</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability", 
            "CA2000:Dispose objects before losing scope",
            Justification = "We manually .Clear() and .Dispose() of hasher in cleanup due to .Dispose() being private on HashAlgorithm")]
        public static string GetFileChecksum(string fileName, ChecksumType checksumType)
        {
            HashAlgorithm hasher = null;
            string hash = String.Empty;

            try
            {
                switch (checksumType)
                {
                    case ChecksumType.MD5:
                        hasher = MD5.Create();
                        break;
                    
                    case ChecksumType.SHA1:
                        hasher = SHA1.Create();
                        break;
                    
                    case ChecksumType.SHA256:
                        hasher = SHA256.Create();
                        break;

                    case ChecksumType.SHA512:
                        hasher = SHA512.Create();
                        break;
                }

                // compute hash from file
                using (StreamReader fileReader = new StreamReader(fileName))
                {
                    hash = ByteHashToString(hasher.ComputeHash(fileReader.BaseStream));
                }
            }
            catch (IOException)
            {
                hash = String.Empty;
            }

            if (hasher != null)
            {
                // Clear() "Releases all resources used by the HashAlgorithm class"
                hasher.Clear();

                // NOTE: HashAlgorithm.Dispose is private, so let's cast to IDisposable anyways and it'll work
                ((IDisposable)hasher).Dispose();
            }

            return hash;
        }
        
        /// <summary>
        /// Convert a hash from bytes to a string
        /// </summary>
        /// <param name="hash">Byte array of hash</param>
        /// <returns>String representation of hash</returns>
        private static string ByteHashToString(byte[] hash)
        {
            StringBuilder str = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                str.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return str.ToString();
        }
    }
}
