using System;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Npgsql;

namespace RohBot
{
    public interface IInsertable
    {
        void Insert(NpgsqlConnection connection, NpgsqlTransaction transaction);
    }

    public class BatchInserter : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly BufferBlock<IInsertable> _buffer;
        private readonly Thread _consumeThread;

        public BatchInserter()
        {
            _cts = new CancellationTokenSource();
            _buffer = new BufferBlock<IInsertable>(new DataflowBlockOptions
            {
                BoundedCapacity = 250
            });

            _consumeThread = new Thread(ConsumeThread);
            _consumeThread.Start();
        }

        public void Dispose()
        {
            _cts.Cancel();
            _buffer.Complete();
            _consumeThread.Join();
        }

        public void Add(IInsertable insertable)
        {
            _buffer.Post(insertable);
        }

        private void ConsumeThread()
        {
            var ct = _cts.Token;

            while (!ct.IsCancellationRequested)
            {
                if (_buffer.Count == 0)
                {
                    Thread.Sleep(100);
                    continue;
                }

                try
                {
                    using (var connection = Database.CreateConnection())
                    using (var transaction = connection.BeginTransaction())
                    {
                        IInsertable insertable;
                        while (_buffer.TryReceive(null, out insertable))
                        {
                            try
                            {
                                insertable.Insert(connection, transaction);
                            }
                            catch (Exception e)
                            {
                                Program.Logger.Error("Batch insert failed", e);
                            }
                        }

                        transaction.Commit();
                    }
                }
                catch (Exception e)
                {
                    Program.Logger.Error("Batch insert database connection failed", e);
                }

                Thread.Sleep(100);
            }
        }
    }
}
