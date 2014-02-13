using RxEF.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;

namespace RxEF
{
    public class Session : ISession
    {
        private DbContext _context;
        private Subject<bool> _isProcessing = new Subject<bool>();
        private IDisposable _workSubscription;
        private ISubject<Action, Action> _subjWork = Subject.Synchronize(new Subject<Action>());
        private ISubject<Exception> _exceptions;

        internal Session(DbContext context, Action<string> logger = null)
        {
            _context = context;
            _context.Database.Log = logger ?? (x => System.Diagnostics.Debug.WriteLine(x));

            _workSubscription = _subjWork
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(x => x());

            IsWorking = _isProcessing
                .Publish(false)
                .PermaRef()
                .DistinctUntilChanged();

            ThrownExceptions = _exceptions = new Subject<Exception>();
        }

        public ITake<TStore> Take<TStore>()
        {
            return new Store<TStore>(_context);
        }

        private ISee<TStore> LookAt<TStore>()
        {
            return new Store<TStore>(_context);
        }

        public IEnumerable<Type> GetAvailableTypes()
        {
            // Get all of the public, instance properties
            return _context.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)

                // Filter by...
                .Where(p =>

                    // Restricting to generically typed properties
                     p.PropertyType.IsGenericType &&

                        // Checking if the property is an IDbSet<> itself, or...
                        ((p.PropertyType.IsInterface && p.PropertyType.GetGenericTypeDefinition() == typeof(IDbSet<>)) ||

                        // Checking if the property implements IDbSet<>
                        (p.PropertyType.GetInterfaces()
                            .Where(m => m.IsGenericType)
                            .Select(m => m.GetGenericTypeDefinition())
                            .Any(m => m == typeof(IDbSet<>)))))

                // Then dump all of the resulting type arguments into one Enumerable
                .SelectMany(x => x.PropertyType.GetGenericArguments());
        }

        public IObservable<TResult> FetchMergedResults<TSource1, TSource2, TResult>(QueryableFunc<TSource1, TSource2, TResult> mergeStrategy)
        {
            return FetchMergedResults(mergeStrategy, new Query<TResult>());
        }

        public IObservable<T> FetchMergedResults<TSource1, TSource2, TResult, T>(QueryableFunc<TSource1, TSource2, TResult> mergeStrategy, IQuery<TResult, T> query)
        {
            return FetchResults(query, () => mergeStrategy(LookAt<TSource1>(), LookAt<TSource2>()));
        }

        public IObservable<T> FetchResults<T>()
        {
            return FetchResults<T, T>(new Query<T>());
        }

        public IObservable<TResult> FetchResults<TSource, TResult>(IQuery<TSource, TResult> query)
        {
            return FetchResults(query, LookAt<TSource>);
        }

        private IObservable<TResult> FetchResults<TSource, TResult>(IQuery<TSource, TResult> query, QueryableFunc<TSource> querySource)
        {
            var subj = new Subject<TResult>();
            _subjWork.OnNext(() =>
            {
                lock (_isProcessing) _isProcessing.OnNext(true);
                try
                {
                    query.Against(querySource())
                        .ToObservable()
                        .Subscribe(subj);
                }
                catch (Exception ex)
                {
                    _exceptions.OnNext(ex);
                }
                finally
                {
                    lock (_isProcessing) _isProcessing.OnNext(false);
                }
            });

            return subj;
        }

        public INotifyWhenComplete ScopedChanges()
        {
            return new ScopeChanges(_context, _subjWork, _isProcessing, _exceptions);
        }

        public IObservable<bool> IsWorking { get; private set; }
        public IObservable<Exception> ThrownExceptions { get; private set; }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);

            // In case subclasses implement a finalizer...
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_context != null)
                        _context.Dispose();

                    _workSubscription.Dispose();
                    _isProcessing.Dispose();
                }
            }
        }

        private bool _disposed = false;
        #endregion

        private sealed class ScopeChanges : INotifyWhenComplete
        {
            public ScopeChanges(DbContext context, ISubject<Action, Action> workQueue, ISubject<bool> isProcessing, ISubject<Exception> exceptions)
            {
                _context = context;
                _workQueue = workQueue;
                _isProcessing = isProcessing;
                _exceptions = exceptions;
                _completion = new Subject<bool>();
            }

            public IObservable<bool> Completion
            {
                get { return _completion; }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_completion")]
            public void Dispose()
            {
                if (_workQueue != null)
                    _workQueue.OnNext(() =>
                    {
                        lock (_isProcessing) { _isProcessing.OnNext(true); }
                        try
                        {
                            _context.SaveChanges();
                            _completion.OnNext(true);
                        }
                        catch (Exception ex)
                        {
                            _completion.OnNext(false);
                            _exceptions.OnNext(ex);
                        }

                        _completion.OnCompleted();
                        _completion.Dispose();
                        lock (_isProcessing) { _isProcessing.OnNext(false); }
                    });
            }

            private DbContext _context;
            private ISubject<Action, Action> _workQueue;
            private ISubject<bool> _isProcessing;
            private Subject<bool> _completion;
            private ISubject<Exception> _exceptions;
        }
    }

    // From ReactivUI
    internal static class PermaRefMixin
    {
        internal static IObservable<T> PermaRef<T>(this IConnectableObservable<T> This)
        {
            This.Connect();
            return This;
        }
    }
}
