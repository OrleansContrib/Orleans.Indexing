using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Orleans.Indexing
{
    public static class OrleansQueryableExtensions
    {
        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        public static IOrleansQueryable<TIGrain, TProperties> Where<TIGrain, TProperties>(this IOrleansQueryable<TIGrain, TProperties> source, Expression<Func<TProperties, bool>> predicate)
            where TIGrain : IIndexableGrain
            => (IOrleansQueryable<TIGrain, TProperties>)Queryable.Where(source, predicate);

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        public static IOrleansQueryable<TIGrain, TProperties> Where<TIGrain, TProperties>(this IOrleansQueryable<TIGrain, TProperties> source, Expression<Func<TProperties, int, bool>> predicate)
            where TIGrain : IIndexableGrain
            => (IOrleansQueryable<TIGrain, TProperties>)Queryable.Where(source, predicate);

        /// <summary>
        /// Sorts the elements of a sequence in ascending order according to a key.
        /// </summary>
        public static IOrleansQueryable<TIGrain, TProperties> OrderBy<TIGrain, TProperties, TK>(this IOrleansQueryable<TIGrain, TProperties> source, Expression<Func<TProperties, TK>> keySelector)
            where TIGrain : IIndexableGrain
            => (IOrleansQueryable<TIGrain, TProperties>)Queryable.OrderBy(source, keySelector);

        /// <summary>
        /// Sorts the elements of a sequence in ascending order according to a key.
        /// </summary>
        public static IOrleansQueryable<TIGrain, TProperties> OrderBy<TIGrain, TProperties, TK>(this IOrleansQueryable<TIGrain, TProperties> source, Expression<Func<TProperties, TK>> keySelector, IComparer<TK> comparer)
            where TIGrain : IIndexableGrain
            => (IOrleansQueryable<TIGrain, TProperties>)Queryable.OrderBy(source, keySelector, comparer);

        /// <summary>
        /// Sorts the elements of a sequence in descending order according to a key.
        /// </summary>
        public static IOrleansQueryable<TIGrain, TProperties> OrderByDescending<TIGrain, TProperties, TK>(this IOrleansQueryable<TIGrain, TProperties> source, Expression<Func<TProperties, TK>> keySelector)
            where TIGrain : IIndexableGrain
            => (IOrleansQueryable<TIGrain, TProperties>)Queryable.OrderByDescending(source, keySelector);

        /// <summary>
        /// Sorts the elements of a sequence in descending order according to a key.
        /// </summary>
        public static IOrleansQueryable<TIGrain, TProperties> OrderByDescending<TIGrain, TProperties, TK>(this IOrleansQueryable<TIGrain, TProperties> source, Expression<Func<TProperties, TK>> keySelector, IComparer<TK> comparer)
            where TIGrain : IIndexableGrain
            => (IOrleansQueryable<TIGrain, TProperties>)Queryable.OrderByDescending(source, keySelector, comparer);

        /// <summary>
        /// Sorts(secondary) the elements of a sequence in ascending order according to a key.
        /// </summary>
        public static IOrleansQueryable<TIGrain, TProperties> ThenBy<TIGrain, TProperties, TK>(this IOrleansQueryable<TIGrain, TProperties> source, Expression<Func<TProperties, TK>> keySelector)
            where TIGrain : IIndexableGrain
            => (IOrleansQueryable<TIGrain, TProperties>)Queryable.ThenBy(source, keySelector);

        /// <summary>
        /// Sorts(secondary) the elements of a sequence in descending order according to a key.
        /// </summary>
        public static IOrleansQueryable<TIGrain, TProperties> ThenByDescending<TIGrain, TProperties, TK>(this IOrleansQueryable<TIGrain, TProperties> source, Expression<Func<TProperties, TK>> keySelector)
            where TIGrain : IIndexableGrain
            => (IOrleansQueryable<TIGrain, TProperties>)Queryable.ThenByDescending(source, keySelector);

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IOrleansQueryable<TResult, TProperties> Select<TSource, TProperties, TResult>(this IOrleansQueryable<TSource, TProperties> source, Expression<Func<TProperties, TResult>> selector) where TSource : IIndexableGrain
            where TResult : IIndexableGrain
            => (IOrleansQueryable<TResult, TProperties>)Queryable.Select(source, selector);

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IOrleansQueryable<TResult, TProperties> Select<TSource, TProperties, TResult>(this IOrleansQueryable<TSource, TProperties> source, Expression<Func<TProperties, int, TResult>> selector) where TSource : IIndexableGrain
            where TResult : IIndexableGrain
            => (IOrleansQueryable<TResult, TProperties>)Queryable.Select(source, selector);

        /// <summary>
        /// Implementation of In operator
        /// </summary>
        public static bool In<TIGrain, TProperties>(this TProperties field, IEnumerable<TProperties> values)
            => values.Any(value => field.Equals(value));

        /// <summary>
        /// Implementation of In operator
        /// </summary>
        public static bool In<TIGrain, TProperties>(this TProperties field, params TProperties[] values)
            => values.Any(value => field.Equals(value));

        /// <summary>
        /// Bypasses a specified number of elements in a sequence and then returns the remaining elements.
        /// </summary>
        public static IOrleansQueryable<TSource, TProperties> Skip<TSource, TProperties>(this IOrleansQueryable<TSource, TProperties> source, int count)
            where TSource : IIndexableGrain
            => (IOrleansQueryable<TSource, TProperties>)Queryable.Skip(source, count);

        public static IOrleansQueryable<TSource, TProperties> Take<TSource, TProperties>(this IOrleansQueryable<TSource, TProperties> source, int count)
            where TSource : IIndexableGrain
            => (IOrleansQueryable<TSource, TProperties>)Queryable.Take(source, count);

        /// <summary>
        /// Implementation of the Contains ANY operator
        /// </summary>
        public static bool ContainsAny<TIGrain, TProperties>(this IEnumerable<TProperties> list, IEnumerable<TProperties> items)
            => throw new InvalidOperationException("This method isn't meant to be called directly, it just exists as a place holder, for the LINQ provider");

        /// <summary>
        /// Implementation of the Contains ALL operatior
        /// </summary>
        public static bool ContainsAll<TIGrain, TProperties>(this IEnumerable<TProperties> list, IEnumerable<TProperties> items)
            => throw new InvalidOperationException("This method isn't meant to be called directly, it just exists as a place holder for the LINQ provider");
    }
}
