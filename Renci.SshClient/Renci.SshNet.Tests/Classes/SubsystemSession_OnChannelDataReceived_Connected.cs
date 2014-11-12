﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Renci.SshNet.Channels;
using Renci.SshNet.Common;

namespace Renci.SshNet.Tests.Classes
{
    [TestClass]
    public class SubsystemSession_OnChannelDataReceived_Connected
    {
        private Mock<ISession> _sessionMock;
        private Mock<IChannelSession> _channelMock;
        private string _subsystemName;
        private SubsystemSessionStub _subsystemSession;
        private TimeSpan _operationTimeout;
        private Encoding _encoding;
        private IList<EventArgs> _disconnectedRegister;
        private IList<ExceptionEventArgs> _errorOccurredRegister;
        private ChannelDataEventArgs _channelDataEventArgs;
        private MockSequence _sequence;

        [TestInitialize]
        public void Setup()
        {
            Arrange();
            Act();
        }

        protected void Arrange()
        {
            var random = new Random();
            _subsystemName = random.Next().ToString(CultureInfo.InvariantCulture);
            _operationTimeout = TimeSpan.FromSeconds(30);
            _encoding = Encoding.UTF8;
            _disconnectedRegister = new List<EventArgs>();
            _errorOccurredRegister = new List<ExceptionEventArgs>();
            _channelDataEventArgs = new ChannelDataEventArgs(
                (uint)random.Next(0, int.MaxValue),
                new[] { (byte)random.Next(byte.MinValue, byte.MaxValue) });

            _sessionMock = new Mock<ISession>(MockBehavior.Strict);
            _channelMock = new Mock<IChannelSession>(MockBehavior.Strict);

            _sequence = new MockSequence();
            _sessionMock.InSequence(_sequence).Setup(p => p.CreateChannelSession()).Returns(_channelMock.Object);
            _channelMock.InSequence(_sequence).Setup(p => p.Open());
            _channelMock.InSequence(_sequence).Setup(p => p.SendSubsystemRequest(_subsystemName)).Returns(true);

            _subsystemSession = new SubsystemSessionStub(
                _sessionMock.Object,
                _subsystemName,
                _operationTimeout,
                _encoding);
            _subsystemSession.Disconnected += (sender, args) => _disconnectedRegister.Add(args);
            _subsystemSession.ErrorOccurred += (sender, args) => _errorOccurredRegister.Add(args);
            _subsystemSession.Connect();
        }

        protected void Act()
        {
            _channelMock.Raise(s => s.DataReceived += null, _channelDataEventArgs);
        }

        [TestMethod]
        public void DisconnectHasNeverFired()
        {
            Assert.AreEqual(0, _disconnectedRegister.Count);
        }

        [TestMethod]
        public void ErrorOccurredHasNeverFired()
        {
            Assert.AreEqual(0, _errorOccurredRegister.Count);
        }

        [TestMethod]
        public void OnDataReceivedShouldBeInvokedOnce()
        {
            Assert.AreEqual(1, _subsystemSession.OnDataReceivedInvocations.Count);

            var received = _subsystemSession.OnDataReceivedInvocations[0];
            Assert.AreEqual(_channelDataEventArgs.Data, received.Data);
            Assert.AreEqual(_channelDataEventArgs.DataTypeCode, received.DataTypeCode);
        }

        [TestMethod]
        public void IsOpenShouldReturnTrueWhenChannelIsOpen()
        {
            _channelMock.InSequence(_sequence).Setup(p => p.IsOpen).Returns(true);

            Assert.IsTrue(_subsystemSession.IsOpen);

            _channelMock.Verify(p => p.IsOpen, Times.Once);
        }

        [TestMethod]
        public void IsOpenShouldReturnFalseWhenSessionIsNotConnected()
        {
            _channelMock.InSequence(_sequence).Setup(p => p.IsOpen).Returns(false);

            Assert.IsFalse(_subsystemSession.IsOpen);

            _channelMock.Verify(p => p.IsOpen, Times.Once);
        }

        [TestMethod]
        public void CloseOnChannelShouldNeverBeInvoked()
        {
            _channelMock.Verify(p => p.Close(), Times.Never);
        }

        [TestMethod]
        public void DisposeOnChannelShouldNeverBeInvoked()
        {
            _channelMock.Verify(p => p.Dispose(), Times.Never);
        }
    }
}
