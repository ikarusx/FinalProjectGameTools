using System;
using System.IO;

namespace BatchFramework
{
    public class FileAccessLogic : IFileAccessLogic
    {
        #region Property Declarations

        private bool _verbose = false;

        private bool _recursive = false;

        private bool _skipReadOnly = false;

        private bool _forceWriteable = false;

        private string _filePattern = "*.*";

        private bool _cancelled = false;

        private bool _running = false;

        #endregion

        #region Event Declarations

        public event FileAccessProcessEventHandler OnProcess = null;

        public event FileAccessNotifyEventHandler OnNotify = null;

        #endregion

        #region IFileAccessLogic Members

        #region Property Declarations

        public bool Verbose
        {
            get { return _verbose; }
            set
            {
                if (!this._running)
                {
                    _verbose = value;
                }
            }
        }

        public bool Recursive
        {
            get { return _recursive; }
            set
            {
                if (!this._running)
                {
                    _recursive = value;
                }
            }
        }

        public bool SkipReadOnly
        {
            get { return _skipReadOnly; }
            set
            {
                if (!this._running)
                {
                    _skipReadOnly = value;
                }
            }
        }

        public bool ForceWriteable
        {
            get { return _forceWriteable; }
            set
            {
                if (!this._running)
                {
                    _forceWriteable = value;
                }
            }
        }

        public string FilePattern
        {
            get { return _filePattern; }
            set
            {
                if (!this._running)
                {
                    _filePattern = value;
                }
            }
        }

        public bool Cancelled
        {
            get { return _cancelled; }
            set { _cancelled = value; }
        }

        #endregion

        public void Execute(string fullPath)
        {
            _cancelled = false;
            _running = true;

            if (File.Exists(fullPath))
            {
                Process(this, new FileInfo(fullPath));
            }
            else if (Directory.Exists(fullPath))
            {
                ProcessDirectory(fullPath);
            }

            _running = false;
        }

        public void Cancel()
        {
            _cancelled = true;
        }

        public void Notify(string message)
        {
            if (_verbose)
            {
                if (this.OnNotify != null)
                {
                    this.OnNotify(this, new NotifyEventArgs(message));
                }
            }
        }

        #endregion

        #region IO Functionality

        private void ProcessDirectory(string directoryPath)
        {
            ProcessDirectory(new DirectoryInfo(directoryPath));
        }

        private void ProcessDirectory(DirectoryInfo directoryInfo)
        {
            if (_cancelled)
            {
                return;
            }

            ProcessFiles(directoryInfo);

            if (_recursive)
            {
                foreach (DirectoryInfo subDirectoryInfo in directoryInfo.GetDirectories())
                {
                    ProcessDirectory(subDirectoryInfo);
                }
            }
        }

        private void ProcessFiles(DirectoryInfo directoryInfo)
        {
            foreach (FileInfo fileInfo in directoryInfo.GetFiles(this._filePattern))
            {
                if (_cancelled)
                {
                    return;
                }

                FileAttributes attributes = File.GetAttributes(fileInfo.FullName);

                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    // skip has precedence over force writeable
                    if (_skipReadOnly)
                    {
                        continue;
                    }
                    else if (_forceWriteable)
                    {
                        File.SetAttributes(fileInfo.FullName, FileAttributes.Normal);
                    }
                    else
                    {
                        continue;
                    }
                }

                Process(this, fileInfo);
            }
        }

        #endregion

        protected virtual void Process(IFileAccessLogic logic, FileInfo fileInfo)
        {
            if (OnProcess != null)
            {
                OnProcess(this, new ProcessEventArgs(this, fileInfo));
            }
        }
    }
}
