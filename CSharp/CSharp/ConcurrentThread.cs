using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSharp
{
    /// <summary>
    /// 批量高并发处理
    /// 即不断增加T
    /// 当达到一定数里或者一定时间时引发批量操作
    /// 这里通过自旋来节约CPU处理时间
    /// 减少独占CPU的时间
    /// </summary>
    public class ConcurrentThread<T>:IDisposable
    {
        private Action<List<T>> _action = null;

        private List<T> _curList = null;

        /// <summary>
        /// 线程同步标志位
        /// </summary>
        private int _flag = 0;

        /// <summary>
        /// 主线程
        /// </summary>
        private Thread _mainThread = null;

        /// <summary>
        /// 当主线程检测在_timeOut时间内，
        /// 满足数量达到或超过_minCount,则触发批量操作
        /// </summary>
        private int _minCount = 0;

        /// <summary>
        /// 当_list数量满足_maxCount时，
        /// 会分出一个短线程，触发批量处理的操作
        /// </summary>
        private int _maxCount = 0;

        /// <summary>
        /// 等待时间，如果过了该时间仍然会触发批量操作
        /// 单位：毫秒
        /// </summary>
        private int _timeOut = 0;

        /// <summary>
        /// 线程开关
        /// </summary>
        private readonly ManualResetEvent _close = new ManualResetEvent(false);

        public ConcurrentThread(Action<List<T>> action,uint min_count,
            uint max_count,int timeout)
        {
            if (_maxCount < min_count)
            {
                throw new ArgumentException("max_count必须大于等于min_count","max_count");
            }
            if (action == null)
            {
                throw new ArgumentException("批量处理委托不能为空", "action");
            }
            _action = action;
            _maxCount = (int)max_count;
            _minCount = (int)min_count;
            _timeOut = timeout;
            //初始化_minCount一定程度上能增加效率
            _curList = new List<T>(_minCount);
        }

        public void Start()
        {
            if (_mainThread == null)
            {
                _close.Reset();
                _mainThread = new Thread(Run);
                _mainThread.IsBackground = true;
                _mainThread.SetApartmentState(ApartmentState.MTA);
                _mainThread.Start();
            }
        }

        public void Stop()
        {
            if (_mainThread != null)
            {
                _close.Set();
                _mainThread.Join();
                _mainThread = null;
            }
        }

        public void Add(T entity)
        {
            //自旋同步锁，如果_flag=0,则条件不成立，_flag=1则条件成立，达到了同步等待的功能
            SpinWait.SpinUntil(() => Interlocked.CompareExchange(ref _flag, 1, 0) == _flag);
            _curList.Add(entity);
            if (_curList.Count >= _maxCount)
            {
                //数里如果满足，则使用系统线程池处理
                var list = _curList;
                ThreadPool.QueueUserWorkItem(RunOnce, list);
                //重置list
                _curList = new List<T>(_minCount);
            }
            //将_flag置回0，释放同步锁
            Interlocked.Exchange(ref _flag, 0);
        }

        private void RunOnce(object state)
        {
            var data = state as List<T>;
            _action(data);
        }

        private void Run()
        {
            while (_close.WaitOne(0))
            {
                //自旋等待,有数据的情况下触发
                SpinWait.SpinUntil(() => _curList.Count > 0||_close.WaitOne(0));
                if (_curList.Count == 0)
                {
                    continue;
                }
                /*
                 * 如果在_timeOut时间内满足list会大于等于_minCount，则退出等待
                 * 如果超出等待时间也进行批量操作
                 * 这样的判断能有效缩短等待的时间长度
                 */
                SpinWait.SpinUntil(() => _curList.Count >= _minCount || _close.WaitOne(0), _timeOut);

                //自旋同步锁，如果_flag=0,则条件不成立，_flag=1则条件成立，达到了同步等待的功能
                SpinWait.SpinUntil(() => Interlocked.CompareExchange(ref _flag, 1, 0) == _flag);
                if (_curList.Count > 0)
                {
                    //触发批量操作
                    var list = _curList;
                    _action(list);
                    _curList = new List<T>();
                }
                //将_flag置回0，释放同步锁
                Interlocked.Exchange(ref _flag, 0);
            }
        }

        public void Dispose()
        {
            try
            {
                Stop();
            }
            finally
            {
                _close.Dispose();
            }
        }
    }
}
