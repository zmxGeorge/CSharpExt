using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSharp.Threading
{
    /// <summary>
    /// 达到一定阈值触发的信号量
    /// </summary>
    public class CountWaitEvent 
    {
        private int _count = 0;

        /// <summary>
        /// 当前阈值
        /// </summary>
        public int CurrentCount { get { return _count; } }

        public CountWaitEvent()
        {
        }

        /// <summary>
        /// 设定一个初始值开始的信号量
        /// </summary>
        /// <param name="init_count"></param>
        public CountWaitEvent(int init_count)
        {
            _count = init_count;
        }

        /// <summary>
        /// 递增信号量
        /// </summary>
        public void AddCount()
        {
            Interlocked.Increment(ref _count);
        }

        /// <summary>
        /// 增加一定数里的信号量
        /// </summary>
        /// <param name="count"></param>
        public void AddCount(int count)
        {
            Interlocked.Exchange(ref _count,count);
        }

        /// <summary>
        /// 无限期等待信号量满足count值
        /// 停止阻塞当前线程
        /// </summary>
        /// <param name="count"></param>
        public void WaitOne(int count)
        {
            SpinWait.SpinUntil(() => _count >= count);
        }

        /// <summary>
        /// 等待信号量满足count值
        /// 或者过了millisecondsTimeout时间
        /// </summary>
        /// <param name="count"></param>
        /// <param name="millisecondsTimeout"></param>
        public bool WaitOne(int count, int millisecondsTimeout)
        {
            return SpinWait.SpinUntil(() => _count >= count, millisecondsTimeout);
        }

        /// <summary>
        /// 等待信号量满足count值
        /// 或者过了timeout时间
        /// </summary>
        /// <param name="count"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool WaitOne(int count, TimeSpan timeout)
        {
            return SpinWait.SpinUntil(() => _count >= count, timeout);
        }


    }
}
