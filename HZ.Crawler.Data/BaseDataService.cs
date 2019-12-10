using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HZ.Crawler.Model;
using Microsoft.EntityFrameworkCore;

namespace HZ.Crawler.Data
{
    public class BaseDataService<T> where T : BaseModel
    {
        private DataContext _context;
        public BaseDataService(DataContext context)
        {
            _context = context;
        }
        private readonly static object Lock_Obj = new object();
        /// <summary>
        /// 查询一个实体对象
        /// </summary>
        /// <param name="whereLambda"></param>
        /// <returns></returns>
        public T Query(Expression<Func<T, bool>> whereLambda)
        {
            T result;
            lock (Lock_Obj)
            {
                result = _context.Set<T>().FirstOrDefaultAsync(whereLambda).GetAwaiter().GetResult();
            }
            return result;
        }
        public IList<T> QueryList(Expression<Func<T, bool>> whereLambda)
        {
            IList<T> result;
            lock (Lock_Obj)
            {
                result = _context.Set<T>().Where(whereLambda).ToListAsync().GetAwaiter().GetResult();
            }
            return result;
        }
        public IList<T> QueryOrderList<TKey>(Expression<Func<T, bool>> whereLambda, Expression<Func<T, TKey>> orderLambda = null)
        {
            var result = _context.Set<T>().Where(whereLambda);
            if (orderLambda != null)
            {
                result = result.OrderBy(orderLambda);
            }
            return result.ToListAsync().GetAwaiter().GetResult();
        }
        /// <summary>
        /// 添加一个实体对象
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public void Add(T model)
        {
            lock (Lock_Obj)
            {
                _context.AddAsync(model).GetAwaiter().GetResult();
            }
        }
        /// <summary>
        /// 添加多个实体对象
        /// </summary>
        /// <param name="modelList"></param>
        public void AddRange(IList<T> modelList)
        {
            lock (Lock_Obj)
            {
                _context.AddRangeAsync(modelList).GetAwaiter().GetResult();
            }
        }
        /// <summary>
        /// 编辑一个实体对象
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool Edit(T model)
        {
            return _context.Update(model) != null;
        }
        /// <summary>
        /// 根据lambda表达式删除
        /// </summary>
        /// <param name="whereLambda"></param>
        /// <returns></returns>
        public void Delete(Expression<Func<T, bool>> whereLambda)
        {
            lock (Lock_Obj)
            {
                var result = QueryList(whereLambda);
                _context.RemoveRange(result);
            }
        }
        public void Commit()
        {
            lock (Lock_Obj)
            {
                _context.SaveChangesAsync().GetAwaiter().GetResult();
            }
        }
    }
}
