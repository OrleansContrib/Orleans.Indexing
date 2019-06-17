using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Orleans.Streams;

namespace Orleans.Indexing
{
    /// <summary>
    /// Implements <see cref="IOrleansQueryProvider"/>
    /// </summary>
    public class OrleansQueryProvider<TIGrain, TProperties> : IOrleansQueryProvider where TIGrain : IIndexableGrain
    {
        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Call && ((MethodCallExpression)expression).Arguments.Count > 0)
            {
                var genericArgs = ((MethodCallExpression)expression).Arguments[0].Type.GetGenericArguments();
                return CreateQuery(expression, genericArgs[0], genericArgs[1]);
            }
            throw new NotSupportedException();
        }

        // Called by System.Linq.Queryable.Where()
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => (IQueryable<TElement>)((IQueryProvider)this).CreateQuery(expression);

        private IQueryable CreateQuery(Expression expression, Type grainInterfaceType, Type iPropertiesType)
        {
            if (expression.NodeType == ExpressionType.Call)
            {
                var methodCall = ((MethodCallExpression)expression);
                if (IsWhereClause(methodCall)
                    && CheckIsOrleansIndex(methodCall.Arguments[0], grainInterfaceType, iPropertiesType, out IIndexFactory indexFactory, out IStreamProvider streamProvider)
                    && methodCall.Arguments[1] is UnaryExpression ue && ue.NodeType == ExpressionType.Quote && ue.Operand.NodeType == ExpressionType.Lambda)
                {
                    var whereClause = (LambdaExpression)ue.Operand;
                    if (TryGetIndexNameAndLookupValue(whereClause, iPropertiesType, out string indexName, out object lookupValue))
                    {
                        var queryIndexedNodeType = typeof(QueryIndexedGrainsNode<,>).MakeGenericType(grainInterfaceType, iPropertiesType);
                        return (IQueryable)Activator.CreateInstance(queryIndexedNodeType, indexFactory, streamProvider, indexName, lookupValue);
                    }
                }
            }
            throw new NotSupportedException();
        }

        private bool CheckIsOrleansIndex(Expression e, Type grainInterfaceType, Type iPropertiesType, out IIndexFactory indexFactory, out IStreamProvider streamProvider)
        {
            if (e.NodeType == ExpressionType.Constant)
            {
                var queryActiveNodeType = typeof(QueryActiveGrainsNode<,>).MakeGenericType(grainInterfaceType, iPropertiesType);
                var valueType = ((ConstantExpression)e).Value.GetType().GetGenericTypeDefinition().MakeGenericType(grainInterfaceType, iPropertiesType);
                if (queryActiveNodeType.IsAssignableFrom(valueType))
                {
                    var qNode = (QueryGrainsNode)((ConstantExpression)e).Value;
                    indexFactory = qNode.IndexFactory;
                    streamProvider = qNode.StreamProvider;
                    return true;
                }
            }
            indexFactory = null;
            streamProvider = null;
            return false;
        }

        private bool IsWhereClause(MethodCallExpression call)
            => call.Arguments.Count == 2 && call.Method.ReflectedType.Equals(typeof(Queryable)) && call.Method.Name == "Where";

        /// <summary>
        /// This method tries to pull out the index name and lookup value from the given expression tree.
        /// </summary>
        /// <param name="exprTree">the given expression tree</param>
        /// <param name="iPropertiesType">the type of the properties that are being queried</param>
        /// <param name="indexName">the index name that is intended to be pulled out of the expression tree.</param>
        /// <param name="lookupValue">the lookup value that is intended to be pulled out of the expression tree.</param>
        /// <returns>determines whether the operation was successful or not</returns>
        private static bool TryGetIndexNameAndLookupValue(LambdaExpression exprTree, Type iPropertiesType, out string indexName, out object lookupValue)
        {
            if (exprTree.Body is BinaryExpression operation)
            {
                if (operation.NodeType == ExpressionType.Equal)
                {
                    // Passing 'value' to avoid CS1628: Cannot pass out or ref parameter to ...
                    bool getLookupValue(Expression valueExpr, out object value)
                    {
                        if (valueExpr is ConstantExpression constantExpr)
                        {
                            value = constantExpr.Value;
                            return true;
                        }
                        if (valueExpr is MemberExpression memberExpr)
                        {
                            object targetObj = Expression.Lambda<Func<object>>(memberExpr.Expression).Compile()();
                            value = ((FieldInfo)memberExpr.Member).GetValue(targetObj);
                            return true;
                        }
                        value = null;
                        return false;
                    }

                    if ((GetIndexName(exprTree, operation.Left, out indexName) && getLookupValue(operation.Right, out lookupValue))
                        || (GetIndexName(exprTree, operation.Right, out indexName) && getLookupValue(operation.Left, out lookupValue)))
                    {
                        return true;
                    }
                }
            }
            throw new NotSupportedException(string.Format("The provided expression is not supported yet: {0}", exprTree));
        }

        /// <summary>
        /// This method tries to pull out the index name from a given field expression.
        /// </summary>
        /// <param name="exprTree">the original expression tree</param>
        /// <param name="fieldExpr">the field expression that should contain the indexed field</param>
        /// <param name="indexName">receives the index name, if <paramref name="fieldExpr"/> is a query property</param>
        /// <returns>A bool value indicating whether the index name was found</returns>
        private static bool GetIndexName(LambdaExpression exprTree, Expression fieldExpr, out string indexName)
        {
            if (fieldExpr is MemberExpression memberExpression)
            {
                ParameterExpression fieldParam = exprTree.Parameters[0];
                Expression innerFieldExpr = memberExpression.Expression;
                if ((innerFieldExpr.NodeType == ExpressionType.Parameter && innerFieldExpr.Equals(fieldParam)) ||
                    (innerFieldExpr.NodeType == ExpressionType.Convert && innerFieldExpr is UnaryExpression ue && ue.Operand.Equals(fieldParam)))
                {
                    indexName = IndexUtils.PropertyNameToIndexName(memberExpression.Member.Name);
                    return true;
                }
            }
            indexName = null;
            return false;
        }

        #region IOrleansQueryProvider

        public object Execute(Expression expression) => throw new NotImplementedException();

        /// <summary>
        /// Executes the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        public TResult Execute<TResult>(Expression expression) => (TResult)Execute(expression);

        #endregion IOrleansQueryProvider
    }
}
