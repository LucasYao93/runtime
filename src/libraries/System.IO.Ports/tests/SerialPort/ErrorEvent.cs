// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    [KnownFailure]
    public class ErrorEvent : PortsTest
    {
        //Maximum time to wait for all of the expected events to be firered
        private const int MAX_TIME_WAIT = 5000;
        private const int NUM_TRYS = 5;

        #region Test Cases
        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void ErrorEvent_RxOver()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                ErrorEventHandler errEventHandler = new ErrorEventHandler(com1);

                Debug.WriteLine("Verifying RxOver event");

                com1.Handshake = Handshake.RequestToSend;
                com1.BaudRate = 115200;
                com2.BaudRate = 115200;
                com1.Open();
                com2.Open();

                //This might not be necessary but it will clear the RTS pin when the buffer is too full
                com1.Handshake = Handshake.RequestToSend;

                com1.ErrorReceived += errEventHandler.HandleEvent;

                //This is overkill should find a more reasonable amount of bytes to write
                com2.BaseStream.Write(new byte[32767], 0, 32767);

                Assert.True(errEventHandler.WaitForEvent(MAX_TIME_WAIT, 1), "Event never occurred");

                while (0 < errEventHandler.NumEventsHandled)
                {
                    errEventHandler.Validate(SerialError.RXOver, -1);
                }

                lock (com1)
                {
                    if (com1.IsOpen)
                        com1.Close();
                }
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ErrorEvent_RxParity()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                ErrorEventHandler errEventHandler = new ErrorEventHandler(com1);

                Debug.WriteLine("Verifying RxParity event");

                com1.DataBits = 7;
                com1.Parity = Parity.Mark;

                com1.Open();
                com2.Open();

                com1.ErrorReceived += errEventHandler.HandleEvent;

                for (int i = 0; i < NUM_TRYS; i++)
                {
                    Debug.WriteLine("Verifying RxParity event try: {0}", i);

                    com2.BaseStream.Write(new byte[8], 0, 8);
                    Assert.True(errEventHandler.WaitForEvent(MAX_TIME_WAIT, 1));

                    while (0 < errEventHandler.NumEventsHandled)
                    {
                        errEventHandler.Validate(SerialError.RXParity, -1);
                    }
                }

                lock (com1)
                {
                    if (com1.IsOpen)
                        com1.Close();
                }
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ErrorEvent_Frame()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                ErrorEventHandler errEventHandler = new ErrorEventHandler(com1);

                Debug.WriteLine("Verifying Frame event");
                com1.DataBits = 7;

                //com1.StopBits = StopBits.Two;
                com1.Open();
                com2.Open();

                com1.ErrorReceived += errEventHandler.HandleEvent;

                //This should cause a frame error since the 8th bit is not set
                //and com1 is set to 7 data bits ao the 8th bit will +12v where
                //com1 expects the stop bit at the 8th bit to be -12v
                var frameErrorBytes = new byte[] { 0x01 };
                for (int i = 0; i < NUM_TRYS; i++)
                {
                    Debug.WriteLine("Verifying Frame event try: {0}", i);

                    com2.BaseStream.Write(frameErrorBytes, 0, 1);
                    errEventHandler.WaitForEvent(MAX_TIME_WAIT, 1);

                    while (0 < errEventHandler.NumEventsHandled)
                    {
                        errEventHandler.Validate(SerialError.Frame, -1);
                    }
                }

                lock (com1)
                {
                    if (com1.IsOpen)
                        com1.Close();
                }
            }
        }
        #endregion

        #region Verification for Test Cases

        private class ErrorEventHandler : TestEventHandler<SerialError>
        {
            public ErrorEventHandler(SerialPort com) : base(com, false, false)
            {
            }

            public void HandleEvent(object source, SerialErrorReceivedEventArgs e)
            {
                HandleEvent(source, e.EventType);
            }
        }
        #endregion
    }
}
