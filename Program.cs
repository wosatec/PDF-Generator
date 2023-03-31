using PDF_POC.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PDF_POC
{
    internal static class Program
    {
        private static string _templatePath;
        private static IEnumerable<string> _pagePaths;
        private static string _dataPath;
        private static string _outputPath;
        private static bool _isDraft = false;

        internal static void Main(string[] args)
        {
            ValidateArguments(args);

            string directory = Path.GetDirectoryName(_outputPath);

            if (!string.IsNullOrWhiteSpace(directory)
                && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Worker.DoWork(
                _templatePath,
                _pagePaths,
                _dataPath,
                _outputPath,
                _isDraft);
        }

        private static void ValidateArguments(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (i + 1 >= args.Length)
                {
                    throw new ArgumentException("unexpected argument as last token: " + args[i]);
                }

                switch (args[i])
                {
                    case "--template":
                        _templatePath = args[i + 1];
                        i++;
                        break;

                    case "--pages":
                        _pagePaths = args[i + 1].Split(",").Where(pagePath => !string.IsNullOrWhiteSpace(pagePath));
                        i++;
                        break;

                    case "--data":
                        _dataPath = args[i + 1];
                        i++;
                        break;

                    case "--output":
                        _outputPath = args[i + 1];
                        i++;
                        break;

                    case "--draft":
                        _isDraft = true;
                        break;

                    default:
                        throw new ArgumentException(string.Format("unknown option: {0}, recognized tokens: {1}", args[i], string.Join("; ", args)));
                }
            }

#if DEBUG
            string directory = Path.GetDirectoryName(_outputPath);
            string fileName = Path.GetFileName(_outputPath);

            fileName = Guid.NewGuid() + "-" + fileName;

            _outputPath = Path.Combine(directory, fileName);
#endif

            ValidateTemplatePath();
            ValidatePagePaths();
            ValidateDataPath();
            ValidateOutptPath();
        }

        private static void ValidateTemplatePath()
        {
            if (string.IsNullOrWhiteSpace(_templatePath))
            {
                throw new ArgumentException("no template provided");
            }

            if (!File.Exists(_templatePath))
            {
                throw new ArgumentException("invalid argument for --template: " + _templatePath);
            }
        }

        private static void ValidatePagePaths()
        {
            if (_pagePaths == null
                || !_pagePaths.Any()
                || _pagePaths.All(path => string.IsNullOrWhiteSpace(path)))
            {
                throw new ArgumentException("no pages provided");
            }

            foreach (string pagePath in _pagePaths)
            {
                if (!File.Exists(pagePath))
                {
                    throw new ArgumentException("invalid argument for --pages: " + pagePath);
                }
            }
        }

        private static void ValidateDataPath()
        {
            if (string.IsNullOrWhiteSpace(_templatePath))
            {
                throw new ArgumentException("no data provided");
            }

            if (!File.Exists(_dataPath))
            {
                throw new ArgumentException("invalid argument for --data: " + _dataPath);
            }
        }

        private static void ValidateOutptPath()
        {
            if (string.IsNullOrWhiteSpace(_outputPath))
            {
                throw new ArgumentException("no output provided");
            }

            if (File.Exists(_outputPath))
            {
                throw new ArgumentException("file in --output already exists: " + _dataPath);
            }
        }
    }
}
