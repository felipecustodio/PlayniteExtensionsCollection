﻿using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using VndbApiDomain.SharedKernel.Entities;

namespace DatabaseCommon
{
    public class LiteDbRepository<T> : IRepository<T> where T : IAggregateRoot
    {
        private readonly LiteDatabase _db;
        private readonly LiteCollection<T> _collection;
        private readonly List<T> _deleteBuffer = new List<T>();
        private readonly List<Guid> _deleteIdsBuffer = new List<Guid>();
        private readonly object _bufferLock = new object();
        private readonly double _bufferProcessTime = 300;
        private readonly Timer _autoProcessTimer;
        public int Count => _collection.Count();

        public LiteDbRepository(string databasePath)
        {
            _db = new LiteDatabase($"Filename={databasePath};Mode=Exclusive");
            _collection = _db.GetCollection<T>(typeof(T).Name + "_collection");
            _collection.EnsureIndex(x => x.Id, true);
            _autoProcessTimer = new Timer(AutoProcessBuffer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        private void StartAutoProcessTimer()
        {
            _autoProcessTimer.Change(TimeSpan.FromMilliseconds(_bufferProcessTime), TimeSpan.FromMilliseconds(_bufferProcessTime));
        }

        private void StopAutoProcessTimer()
        {
            _autoProcessTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        private void AutoProcessBuffer(object state)
        {
            void ProcessDeleteBuffer()
            {
                if (_deleteBuffer.Count > 0)
                {
                    try
                    {
                        foreach (var item in _deleteBuffer)
                        {
                            _collection.Delete(item.Id);
                        }
                    }
                    finally
                    {
                        _deleteBuffer.Clear();
                    }
                }
            }

            void ProcessDeleteIdsBuffer()
            {
                if (_deleteIdsBuffer.Count > 0)
                {
                    try
                    {
                        foreach (var id in _deleteIdsBuffer)
                        {
                            _collection.Delete(id);
                        }
                    }
                    finally
                    {
                        _deleteIdsBuffer.Clear();
                    }
                }
            }

            lock (_bufferLock)
            {
                StopAutoProcessTimer();

                ProcessDeleteBuffer();
                ProcessDeleteIdsBuffer();
            }
        }

        public void Insert(T item)
        {
            _collection.Insert(item);
        }

        public void InsertBulk(IEnumerable<T> items)
        {
            _collection.InsertBulk(items);
        }

        public int DeleteAll()
        {
            return _collection.Delete(x => true);
        }

        public T GetById(BsonValue id)
        {
            return _collection.FindById(id);
        }

        public bool ExistsById(BsonValue id)
        {
            return GetById(id) != null;
        }

        public T GetOrCreateById(BsonValue id)
        {
            T existingItem = GetById(id);
            if (existingItem != null)
            {
                return existingItem;
            }
            else
            {
                var newItem = Activator.CreateInstance<T>();
                newItem.Id = id;
                Insert(newItem);
                return newItem;
            }
        }

        public IEnumerable<T> GetAll()
        {
            return _collection.FindAll();
        }

        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
        {
            return _collection.Find(predicate);
        }

        public bool Update(T item)
        {
            return _collection.Update(item);
        }

        public bool InsertOrReplace(T item)
        {
            var existingItem = GetById(item.Id);
            if (existingItem == null || Delete(existingItem))
            {
                _collection.Insert(item);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Delete(BsonValue id)
        {
            return _collection.Delete(id);
        }

        public bool Delete(T item)
        {
            return Delete(item.Id);
        }

        public void AddToDeleteBuffer(T item)
        {
            lock (_bufferLock)
            {
                _deleteBuffer.Add(item);
                StartAutoProcessTimer();
            }
        }

        public void AddToDeleteBuffer(BsonValue Id)
        {
            lock (_bufferLock)
            {
                _deleteIdsBuffer.Add(Id);
                StartAutoProcessTimer();
            }
        }

        public IEnumerable<T> GetByIds(IEnumerable<BsonValue> ids)
        {
            var idSet = new HashSet<BsonValue>(ids);
            return _collection.Find(x => idSet.Contains(x.Id));
        }

        public IEnumerable<T> GetByIds(IEnumerable<string> ids)
        {
            return GetByIds(ids.Select(id => new BsonValue(id)));
        }

        public void Dispose()
        {
            _db.Dispose();
            _autoProcessTimer.Dispose();
        }
    }
}