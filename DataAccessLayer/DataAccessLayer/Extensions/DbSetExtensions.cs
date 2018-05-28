﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.EfStructures.Extensions
{
    public static class DbSetExtensions
    {
        public static DbContext GetContext<TEntity>(this DbSet<TEntity> dbSet) where TEntity : class
        {
            return (DbContext)dbSet
                .GetType().GetTypeInfo()
                .GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(dbSet);
        }

        public static Expression<Func<T, bool>> ConvertToWhereClause<T>(this Expression<Func<T, object>> exp, T obj) where T: class,new()
        {
            var memberExp = (MemberExpression)exp.Body;
            var objPropExp = Expression.PropertyOrField(Expression.Constant(obj), memberExp.Member.Name);
            var equalExp = Expression.Equal(exp.Body, objPropExp);
            var exp2 = Expression.Lambda<Func<T, bool>>(equalExp, exp.Parameters);
            return exp2;

        }

        public static void AddOrUpdate<T>(this DbSet<T> dbSet,T data, params Expression<Func<T, object>>[] wheres) where T : class, new()
        {
            AddOrUpdate(dbSet,new List<T>{data},wheres);
        }
        public static void AddOrUpdate<T>(this DbSet<T> dbSet, List<T> data, params Expression<Func<T, object>>[] wheres) where T : class, new()
        {
            var context = dbSet.GetContext();
            foreach (var item in data)
            {
                var query = context.Set<T>().AsNoTracking();
                foreach (var where in wheres)
                {
                    query = query.Where(where.ConvertToWhereClause(item));
                }
                var entity = query.FirstOrDefault();
                if (entity == null)
                {
                    dbSet.Add(item);
                }
                else
                {
                    var ids = context.Model.FindEntityType(typeof(T)).FindPrimaryKey().Properties.Select(x => x.Name);
                    var t = typeof(T);
                    List<PropertyInfo> keyFields = t.GetProperties().Where(x => ids.Contains(x.Name)).ToList();
                    foreach (var k in keyFields)
                    {
                        k.SetValue(item, k.GetValue(entity));
                    }
                    dbSet.Update(item);
                }
            }
        }
    }
}