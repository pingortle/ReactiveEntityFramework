using RxEF.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxEF
{
    public class Query<TSource, TResult> : IQuery<TSource, TResult>
    {
        public Query(QueryableFunc<TSource, TResult> applyQuery)
        {
            ApplyQuery = applyQuery ?? (x => { throw new InvalidOperationException("Query must have a query function."); });
        }

        public IEnumerable<TResult> Against(IQueryable<TSource> source)
        {
            return ApplyQuery(source).ToList();
        }

        public IQuery<TSource, TOut> With<TOut>(QueryableFunc<TResult, TOut> query)
        {
            return new Query<TSource, TOut>(x => query(ApplyQuery(x)));
        }

        protected QueryableFunc<TSource, TResult> ApplyQuery;
    }

    public class Query<T> : Query<T, T>, IQuery<T>
    {
        public Query(QueryableFunc<T, T> applyQuery = null)
            : base(null)
        {
            ApplyQuery = applyQuery ?? (q => q);
        }

        public IQuery<T> With(QueryableFunc<T, T> query)
        {
            return new Query<T>(x => query(ApplyQuery(x)));
        }
    }
}
