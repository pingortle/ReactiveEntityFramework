using RxEF.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace RxEF
{
    internal sealed class Store<T> : IStore<T>
    {
        private DbContext _context;
        private DbSet _dbSet;

        internal Store(DbContext context)
        {
            _context = context;
            _dbSet = _context.Set(typeof(T));
        }

        #region IStore<>
        public void Add(T item)
        {
            _dbSet.Add(item);
        }

        public void Remove(T item)
        {
            _dbSet.Remove(item);
        }

        public void Update(T item)
        {
            _dbSet.Attach(item);
        }

        public void Attach(T item)
        {
            _dbSet.Attach(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)_dbSet.AsQueryable().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _dbSet.AsQueryable().GetEnumerator();
        }

        public Type ElementType
        {
            get { return _dbSet.AsQueryable().ElementType; }
        }

        public System.Linq.Expressions.Expression Expression
        {
            get { return _dbSet.AsQueryable().Expression; }
        }

        public IQueryProvider Provider
        {
            get { return _dbSet.AsQueryable().Provider; }
        }
        #endregion
    }
}
