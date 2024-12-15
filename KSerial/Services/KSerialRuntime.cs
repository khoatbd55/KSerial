using KSerial.Events;
using KSerial.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KUtilities.TaskExtentions;
using KSerial.Models.Message;
using KSerial.Models.ConfigOption;

namespace KSerial.Services
{
    public class KSerialRuntime
    {
        readonly KAsyncEvent<KSerialLine_EventArgs> _lineEvent = new KAsyncEvent<KSerialLine_EventArgs>();
        readonly KAsyncEvent<KSerialLog_EventArg> _logEvent = new KAsyncEvent<KSerialLog_EventArg>();
        readonly KAsyncEvent<KSerialException_EventArg> _exceptionEvent = new KAsyncEvent<KSerialException_EventArg>();
        readonly KAsyncEvent<KSerialClosedConnection_EventArgs> _closedConnectionEvent = new KAsyncEvent<KSerialClosedConnection_EventArgs>();

        public event Func<KSerialLine_EventArgs, Task> OnLineAsync
        {
            add => _lineEvent.AddHandler(value);
            remove => _lineEvent.RemoveHandler(value);
        }

        public event Func<KSerialLog_EventArg, Task> OnLogAsync
        {
            add => _logEvent.AddHandler(value);
            remove => _logEvent.RemoveHandler(value);
        }

        public event Func<KSerialException_EventArg, Task> OnExceptionAsync
        {
            add => _exceptionEvent.AddHandler(value);
            remove => _exceptionEvent.RemoveHandler(value);
        }

        public event Func<KSerialClosedConnection_EventArgs, Task> OnClosedConnectionAsync
        {
            add => _closedConnectionEvent.AddHandler(value);
            remove => _closedConnectionEvent.RemoveHandler(value);
        }

        KNohmiTransport _transport;
        CancellationTokenSource _backgroundCancelTokenSource = new CancellationTokenSource();
        Task _taskEvent;
        Task _taskAutoReconnect;
        Task _taskStop;
        Task _taskSend;
        Task _taskDecode;
        KAsyncQueue<Exception> _stopQueue = new KAsyncQueue<Exception>();
        object _lockStop = new object();
        int _isComportOpened = 0;
        public bool IsRunning { get; private set; } = false;

        KAsyncQueue<string> _sendMsgQueue = new KAsyncQueue<string>();
        KAsyncQueue<KSerialTransport_EventArgs> _transportMsgQueue = new KAsyncQueue<KSerialTransport_EventArgs>(50000);
        KAsyncQueue<KSerialBaseMessage> _eventQueue = new KAsyncQueue<KSerialBaseMessage>(50000);
        KNohmiSerialOptions _option;
        public void Run(KNohmiSerialOptions option)
        {
            this._option = option;

            _backgroundCancelTokenSource = new CancellationTokenSource();
            CancellationToken c = _backgroundCancelTokenSource.Token;
            _stopQueue = new KAsyncQueue<Exception>(100);
            _transportMsgQueue = new KAsyncQueue<KSerialTransport_EventArgs>(50000);
            _eventQueue = new KAsyncQueue<KSerialBaseMessage>(50000);
            _sendMsgQueue = new KAsyncQueue<string>();
            _taskEvent = Task.Run(() => ProcessInflightEvent(c), c);
            _taskStop = Task.Run(() => ProcessStopAllTask(c), c);
            _taskSend = Task.Run(() => ProcessSendCmd(c), c);
            _taskAutoReconnect = Task.Run(() => ProcessAutoReconnect(c), c);
            _taskDecode = Task.Run(() => ProcessDecode(c), c);
            WriteLog("kserial running...");
        }

        private async Task ProcessSendCmd(CancellationToken c)
        {
            while (!c.IsCancellationRequested)
            {
                try
                {
                    var msgQueue = await _sendMsgQueue.TryDequeueAsync(c).ConfigureAwait(false);
                    if (msgQueue.IsSuccess)
                    {
                        await _transport.SendCommandAsync(msgQueue.Item);
                    }    
                }
                catch (Exception ex)
                {
                    WriteLog("exception send message kserial", ex);
                }
            }    
        }

        private async Task ProcessDecode(CancellationToken c)
        {
            while (!c.IsCancellationRequested)
            {
                try
                {
                    var msgQueue = await _transportMsgQueue.TryDequeueAsync(c).ConfigureAwait(false);
                    if (msgQueue.IsSuccess)
                    {
                        EnqueueMessage(new KSerialLineMessage()
                        {
                            Message=msgQueue.Item.Message,
                            Raw=msgQueue.Item.Raw,
                        });
                    }
                }
                catch (Exception ex)
                {
                    WriteLog("exception decode message kserial", ex);
                }
            }
        }

        private async Task ProcessAutoReconnect(CancellationToken c)
        {
            while (!c.IsCancellationRequested)
            {
                try
                {
                    if (_isComportOpened == 0)
                    {
                        WriteLog("reconnect kserial");
                        // cố gắng mở lại kết nối
                        SerialPortStart();
                        IsRunning = true;
                        Interlocked.Exchange(ref _isComportOpened, 1);// trạng thái báo hiệu comport đã mở
                        WriteLog("reconnect success, kserial running...");
                    }
                }
                catch (Exception ex)
                {
                    IsRunning = false;
                    WriteLog("exception auto reconnect kserial ", ex);
                }
                await Task.Delay(500, c).ConfigureAwait(false);
            }
        }


        private async Task ProcessInflightEvent(CancellationToken c)
        {
            while (!c.IsCancellationRequested)
            {
                try
                {
                    var msgQueue = await _eventQueue.TryDequeueAsync(c).ConfigureAwait(false);
                    if (msgQueue.IsSuccess)
                    {
                        var eventData = msgQueue.Item;
                        if (eventData is KSerialLineMessage point)
                        {
                            if (_lineEvent.HasHandlers)
                            {
                                await _lineEvent.InvokeAsync(
                                    new KSerialLine_EventArgs(this, point)).ConfigureAwait(false);
                            }
                        }
                        else if (eventData is KSerialLogMessage log)
                        {
                            if (_logEvent.HasHandlers)
                            {
                                await _logEvent.InvokeAsync(new KSerialLog_EventArg(this, log.Content)).ConfigureAwait(false);
                            }
                        }
                        else if (eventData is KSerialExceptionMessage ex)
                        {
                            if (_exceptionEvent.HasHandlers)
                            {
                                await _exceptionEvent.InvokeAsync(new KSerialException_EventArg(this, ex.Ex)).ConfigureAwait(false);
                            }
                        }
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        public void EnqueueSend(string cmd)
        {
            if(_isComportOpened!=0)
            {
                _sendMsgQueue?.Enqueue(cmd);
            }    
            
        }

        private void EnqueueMessage(KSerialBaseMessage eventData)
        {
            _eventQueue.Enqueue(eventData);
        }

        private void WriteLog(string message, Exception ex)
        {
            EnqueueMessage(new KSerialExceptionMessage(DateTime.Now, ex, message));
        }

        private void WriteLog(string message)
        {
            EnqueueMessage(new KSerialLogMessage(DateTime.Now, message));
        }

        private void SerialPortStart()
        {
            _transport = new KNohmiTransport();
            _transport.Closed += _transport_Closed;
            _transport.OnExceptionOccur += _transport_OnExceptionOccur;
            _transport.OnMessageRecieved += _transport_OnMessageRecieved;
            _transport.Open(_option);
        }

        private void _transport_OnMessageRecieved(object sender, Events.KSerialTransport_EventArgs e)
        {
            _transportMsgQueue.Enqueue(e);
        }

        private void _transport_OnExceptionOccur(object sender, Exception ex)
        {
            KSerialExceptionMessage msgEvent = new KSerialExceptionMessage(DateTime.Now, ex, "Exception serial port");
            EnqueueMessage(msgEvent);
        }

        private void _transport_Closed(object sender, EventArgs e)
        {
            Interlocked.Exchange(ref _isComportOpened, 0);
        }

        private async Task ProcessStopAllTask(CancellationToken c)
        {
            try
            {
                while (!c.IsCancellationRequested)
                {
                    var stop = await _stopQueue.TryDequeueAsync(c).ConfigureAwait(false);
                    if (stop.IsSuccess)
                    {
                        await CloseCoreAsync(stop.Item).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public async Task CloseCoreAsync(Exception ex)
        {
            lock (_lockStop)
            {
                _backgroundCancelTokenSource?.Cancel();
            }
            await _transport.DisconnectAsync().ConfigureAwait(false);
            IsRunning = false;
            await WaitForTask(_taskAutoReconnect).ConfigureAwait(false);
            await WaitForTask(_taskEvent).ConfigureAwait(false);
            await WaitForTask(_taskDecode).ConfigureAwait(false);
            _transportMsgQueue.Clear();
            _eventQueue.Clear(); 
            _sendMsgQueue.Clear();
            if (_closedConnectionEvent.HasHandlers)
            {
                await _closedConnectionEvent.InvokeAsync(new KSerialClosedConnection_EventArgs(this, new EventArgs())).ConfigureAwait(false);
            }
        }

        private void OnClosing(Exception e)
        {
            lock (_lockStop)
            {
                if (!_backgroundCancelTokenSource.Token.IsCancellationRequested)
                {
                    _stopQueue.Enqueue(e);
                }
            }

        }

        public async Task DisconnectAsync()
        {
            this.OnClosing(new Exception("disconnect by require"));
            await WaitForTask(_taskStop).ConfigureAwait(false);
        }

        private async Task WaitForTask(Task task)
        {
            try
            {
                if (task != null && !task.IsCompleted)
                    await task.ConfigureAwait(false);
            }
            catch (Exception)
            {

            }
        }
    }
}
