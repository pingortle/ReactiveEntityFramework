using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;

namespace RxEF.Interfaces
{
    public delegate IQueryable<TResult> QueryableFunc<in TSource1, in TSource2, out TResult>(IQueryable<TSource1> queryable1, IQueryable<TSource2> queryable2);
    public delegate IQueryable<TResult> QueryableFunc<in TSource, out TResult>(IQueryable<TSource> queryable);
    public delegate IQueryable<TResult> QueryableFunc<out TResult>();

    public interface ISession : IDisposable
    {
        ITake<T> Take<T>();

        IEnumerable<Type> GetAvailableTypes();

        INotifyWhenComplete ScopedChanges();

        IObservable<T> FetchResults<T>();
        IObservable<TResult> FetchResults<TSource, TResult>(IQuery<TSource, TResult> query);

        IObservable<T> FetchMergedResults<TSource1, TSource2, TResult, T>(
            QueryableFunc<TSource1, TSource2, TResult> mergeStrategy,
            IQuery<TResult, T> query);

        IObservable<TResult> FetchMergedResults<TSource1, TSource2, TResult>(QueryableFunc<TSource1, TSource2, TResult> mergeStrategy);

        IObservable<bool> IsWorking { get; }
        IObservable<Exception> ThrownExceptions { get; }
    }

    public interface ISee<out T> : IQueryable<T>, IEnumerable<T> { }

    public interface ITake<in T>
    {
        void Add(T item);
        void Remove(T item);
        void Update(T item);
        void Attach(T item);
    }

    public interface IStore<in T1, out T2> : ITake<T1>, ISee<T2>
    {}

    public interface IStore<T> : IStore<T, T> { }

    public interface IQuery<in TSource, out TResult>
    {
        IEnumerable<TResult> Against(IQueryable<TSource> source);
        IQuery<TSource, T> With<T>(QueryableFunc<TResult, T> query);
    }

    public interface IQuery<T> : IQuery<T, T>
    {
        IQuery<T> With(QueryableFunc<T, T> query);
    }

    public interface INotifyWhenComplete : IDisposable
    {
        IObservable<bool> Completion { get; }
    }
}
