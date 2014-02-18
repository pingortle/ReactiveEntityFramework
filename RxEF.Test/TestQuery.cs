using NUnit.Framework;
using System;

using RxEF;
using RxEF.Interfaces;
using System.Linq;
using System.Linq.Expressions;

namespace RxEF.Test
{
	[TestFixture]
	public class TestQuery
	{
		[Test]
		public void Against_Default ()
		{
			var q = new Query<int> ();
			var collection = (new[] { 1, 2, 3, 4, }).AsQueryable();

			CollectionAssert.AreEquivalent (collection, q.Against (collection));
		}

		[Test]
		public void Against_WhereLessThan3 ()
		{
			Func<int, bool> predicate =  x => x < 3;
			QueryableFunc<int, int> queryable = q => q.Where(predicate).AsQueryable();
			var query = new Query<int> (queryable);
			var collection = (new[] { 1, 2, 3, 4, }).AsQueryable();

			CollectionAssert.AreEquivalent (collection.Where(predicate), query.Against (collection));
		}

		[Test]
		public void With_WhereLessThan3ReverseOrder ()
		{
			Func<int, bool> predicate =  x => x < 3;
			QueryableFunc<int, int> queryable = q => q.Where(predicate).AsQueryable();
			var query = new Query<int> (queryable);
			var collection = (new[] { 1, 2, 3, 4, }).AsQueryable();

			CollectionAssert.AreEqual (collection.Where(predicate).OrderByDescending(x => x), query.With(q => q.OrderByDescending(x => x)).Against (collection));
		}
	}
}

