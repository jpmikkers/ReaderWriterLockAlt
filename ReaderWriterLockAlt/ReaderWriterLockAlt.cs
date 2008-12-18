using System;
using System.Collections.Generic;
using System.Threading;

namespace CodePlex.JPMikkers
{
    public class ReaderWriterLockAlt
    {
        private class LockDisposer : IDisposable
        {
            public delegate void VoidDelegate();
            private readonly VoidDelegate m_action;

            public LockDisposer(VoidDelegate action)
            {
                m_action = action;
            }

            public void Dispose()
            {
                m_action();
            }
        }

        //Regarding Thread IDs returned by Thread.ManagedThreadId:
        //The legal range of values is 1 through the maximum value a DWORD (32-bits) can hold, i.e. 2,147,483,647. 
        //Negative values and 0 are not possible. Note that the runtime can also recycle thread IDs over time, so 
        //a thread ID is never guaranteed to be unique throughout time; we do insure a thread ID doesn't get recycled 
        //when a thread with that ID is still active, however.
        //Hope this helps. Regards,
        //joe duffy
        //program manager
        //common language runtime

        private const int NO_THREAD = 0;
        private readonly object m_Sync = new object();
        private int m_LockCount = 0;
        private int waitingReaders = 0;
        private int waitingWriters = 0;
        private int writerThreadId = NO_THREAD;

        public ReaderWriterLockAlt()
        {
        }

        public void EnterReadLock()
        {
            int currentId = Thread.CurrentThread.ManagedThreadId;

            lock (m_Sync)
            {
                // wait until there are no more pending writers, and no writer other than me has the lock
                if (!ReadLockPreCondition(currentId))
                {
                    try
                    {
                        waitingReaders++;
                        do { Monitor.Wait(m_Sync); } while (!ReadLockPreCondition(currentId));
                    }
                    finally
                    {
                        // make sure the waiting counter is updated correctly even in case of an exception
                        waitingReaders--;
                    }
                }
                // the following only gets executed if no exception was thrown:
                m_LockCount++;
            }
        }

        public void ExitReadLock()
        {
            lock (m_Sync)
            {
                if (m_LockCount > 0)
                {
                    m_LockCount--;
                    if (m_LockCount == 0)
                    {
                        PulseWaitingThreads();
                    }
                }
                else
                {
                    throw new SynchronizationLockException("Unbalanced acquire/release read lock detected");
                }
            }
        }

        public void EnterWriteLock()
        {
            int currentId = Thread.CurrentThread.ManagedThreadId;

            lock (m_Sync)
            {
                // Wait for other readers or writers to become ready
                if (!WriteLockPreCondition(currentId))
                {
                    try
                    {
                        waitingWriters++;
                        do { Monitor.Wait(m_Sync); } while (!WriteLockPreCondition(currentId));
                    }
                    finally
                    {
                        // make sure the waiting counter is updated correctly even in case of an exception
                        waitingWriters--;
                    }
                }
                // the following only gets executed if no exception was thrown:
                m_LockCount++;
                writerThreadId = currentId;
            }
        }

        public void ExitWriteLock()
        {
            int currentId = Thread.CurrentThread.ManagedThreadId;

            lock (m_Sync)
            {
                if (m_LockCount > 0)
                {
                    if (writerThreadId == currentId)
                    {
                        // writelock was owned by me
                        m_LockCount--;

                        if (m_LockCount == 0)
                        {
                            writerThreadId = NO_THREAD;
                            PulseWaitingThreads();
                        }
                    }
                    else
                    {
                        throw new SynchronizationLockException("The calling thread attempted to release a write lock but does not own the lock for the specified object");
                    }
                }
                else
                {
                    throw new SynchronizationLockException("Unbalanced acquire/release write lock detected");
                }
            }
        }

        private void PulseWaitingThreads()
        {
            if (waitingReaders > 0 || waitingWriters > 0)
            {
                Monitor.PulseAll(m_Sync);
            }
        }

        private bool WriteLockPreCondition(int currentThreadId)
        {
            return (m_LockCount == 0 || writerThreadId == currentThreadId);
        }

        private bool ReadLockPreCondition(int currentThreadId)
        {
            return (waitingWriters == 0 && (writerThreadId == NO_THREAD || writerThreadId == currentThreadId));
        }

        public IDisposable ReadLock()
        {
            EnterReadLock();
            return new LockDisposer(delegate() { ExitReadLock(); });
        }

        public IDisposable WriteLock()
        {
            EnterWriteLock();
            return new LockDisposer(delegate() { ExitWriteLock(); });
        }
    }
}
