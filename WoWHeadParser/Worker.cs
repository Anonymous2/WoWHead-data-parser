﻿using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WoWHeadParser.Parser;

namespace WoWHeadParser
{
    public class Worker : IDisposable
    {
        private uint _start;
        private uint _end;
        private bool _isWorking;
        private uint[] _entries;
        private Uri _address;
        private ParsingType _type;
        private PageParser _parser;
        private DateTime _timeStart;
        private DateTime _timeEnd;
        private ServicePoint _service;
        private SemaphoreSlim _semaphore;
        private ConcurrentQueue<uint> _badIds;

        private const int SemaphoreCount = 100;
        private const int KeepAliveTime = 100000;

        private object _locker = new object();

        public Worker(ParsingType type, PageParser parser, EventHandler onPageDownloadingComplete = null)
        {
            _type = type;
            _parser = parser;

            _address = LocaleMgr.GetAddress(parser.Locale);
            ServicePointManager.DefaultConnectionLimit = SemaphoreCount * 10;
            {
                _service = ServicePointManager.FindServicePoint(_address);
                _service.SetTcpKeepAlive(true, KeepAliveTime, KeepAliveTime);
            }
            _address = new Uri(_address, parser.Address);

            _semaphore = new SemaphoreSlim(SemaphoreCount, SemaphoreCount);
            _badIds = new ConcurrentQueue<uint>();

            PageDownloadingComplete += onPageDownloadingComplete;
        }

        public void SetValue(uint value)
        {
            _start = value;
        }

        public void SetValue(uint start, uint end)
        {
            _end = end;
            _start = start;
        }

        public void SetValue(uint[] entries)
        {
            _entries = entries;
        }

        public void Start()
        {
            if (_isWorking)
                throw new InvalidOperationException("_isWorking");

            _isWorking = true;
            _timeStart = DateTime.Now;

            switch (_type)
            {
                case ParsingType.TypeBySingleValue:
                    {
                        Process(_start);
                        break;
                    }
                case ParsingType.TypeByMultipleValue:
                    {
                        for (uint entry = _start; entry <= _end; ++entry)
                        {
                            if (!Process(entry))
                                break;
                        }
                        break;
                    }
                case ParsingType.TypeByList:
                    {
                        for (int i = 0; i < _entries.Length; ++i)
                        {
                            if (!Process(_entries[i]))
                                break;
                        }
                        break;
                    }
                case ParsingType.TypeByWoWHeadFilter:
                    {
                        for (uint entry = 0; entry <= _start; ++entry)
                        {
                            if (!Process(entry))
                                break;
                        }
                        break;
                    }
            }

            while (_semaphore.CurrentCount != SemaphoreCount)
            {
                Application.DoEvents();
            }

            if (_type == ParsingType.TypeByList || _type == ParsingType.TypeByWoWHeadFilter)
            {
                while (!_badIds.IsEmpty)
                {
                    if (!_isWorking)
                        break;

                    uint id;
                    if (!_badIds.TryDequeue(out id))
                        continue;

                    if (!Process(id))
                        break;
                }
            }

            while (_semaphore.CurrentCount != SemaphoreCount)
            {
                Application.DoEvents();
            }

            _timeEnd = DateTime.Now;
        }

        private bool Process(uint id)
        {
            if (!_isWorking)
                return false;

            _semaphore.Wait();

            Requests request = new Requests(_address, id, _type == ParsingType.TypeByWoWHeadFilter);
            request.BeginGetResponse(RespCallback, request);
            return true;
        }

        private void RespCallback(IAsyncResult iar)
        {
            Requests request = (Requests)iar.AsyncState;

            string page;
            bool endGetResponse = request.EndGetResponse(iar, out page);
            lock (_locker)
            {
                if (endGetResponse)
                    _parser.TryParse(page, request.Id);
                else
                    _badIds.Enqueue(request.Id);
            }
            request.Dispose();
            _semaphore.Release();

            if (endGetResponse && PageDownloadingComplete != null)
                PageDownloadingComplete(null, EventArgs.Empty);
        }

        public void Stop()
        {
            _isWorking = false;
            _service.ConnectionLeaseTimeout = 0;
        }

        public void Reset()
        {
            _parser = null;
            _isWorking = false;
            _service.ConnectionLeaseTimeout = 0;

            GC.Collect();
        }

        public void Dispose()
        {
            _parser = null;
            _semaphore.Dispose();
            _service.ConnectionLeaseTimeout = 0;
        }

        public override string ToString()
        {
            StringBuilder content = new StringBuilder(_parser.Builder.Count * 256);

            content.AppendFormat(@"-- Dump of {0} ({1}), Total object count: {2}", _timeEnd, _timeEnd - _timeStart, _parser.Builder.Count).AppendLine();
            content.AppendLine();
            content.Append(_parser.ToString());

            return content.ToString();
        }

        public event EventHandler PageDownloadingComplete;
    }
}