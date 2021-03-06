﻿using UnityEngine;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class ControlDevices
{
    public static string ArrowKeys = "arrows";
    public static string WasdKeys = "wasd";
    public static string Random = "random";
}

public class RobotController : MonoBehaviour
{
    private const int FULL_TORQUE = 3;
    public string Control;
    public int Port = 0;

    public WheelCollider[] rightWheels;
    public WheelCollider[] leftWheels;
    Thread _receiveThread;
    UdpClient _socket;
    private float _leftTorque = 0;
    private float _rightTorque = 0;
    private float _lastUpdate = 0;

    void ListenForUDP()
    {
        _socket.BeginReceive(new AsyncCallback(ReceiveData), new {});
    }

    void ParseCommand(string command)
    {
        var commandValues = command.Split(new char[]{',', ';'});
        if (commandValues.Length > 1)
        {
            Int32.TryParse(commandValues[0], out var leftCommand);
            Int32.TryParse(commandValues[1], out var rightCommand);
            _leftTorque = Utils.MapAndLimit(leftCommand, -255, 255, -FULL_TORQUE, FULL_TORQUE);
            _rightTorque = Utils.MapAndLimit(rightCommand, -255, 255, -FULL_TORQUE, FULL_TORQUE);
        }
    }

    void ReceiveData(IAsyncResult result)
    {
        var anyIP = new IPEndPoint(IPAddress.Any, 0);
        var data = _socket.EndReceive(result, ref anyIP);
        var command = Encoding.UTF8.GetString(data);
        Debug.Log($"Recieved upd message: {command}");
        ParseCommand(command);
        ListenForUDP();
    }

    void ListenArrowKeys()
    {
        ListenKeys(KeyCode.UpArrow, KeyCode.LeftArrow, KeyCode.DownArrow, KeyCode.RightArrow);
    }

    void ListenWasdKeys()
    {
        ListenKeys(KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D);
    }

    void ListenKeys(KeyCode up, KeyCode left, KeyCode down, KeyCode right)
    {
        _leftTorque = 0;
        _rightTorque = 0;
        var direction = 0;

        if (Input.GetKey(up))
        {
            direction = 1;
        }
        if (Input.GetKey(down))
        {
            direction = -1;
        }
        _leftTorque = direction * FULL_TORQUE;
        _rightTorque = direction * FULL_TORQUE;

        if (Input.GetKey(left))
        {
            if (direction == 0)
            {
                _leftTorque = -FULL_TORQUE;
                _rightTorque = FULL_TORQUE;
                return;
            }
            _leftTorque = 0;
        }
        if (Input.GetKey(right))
        {
            if (direction == 0)
            {
                _leftTorque = FULL_TORQUE;
                _rightTorque = -FULL_TORQUE;
                return;
            }
            _rightTorque = 0;
        }
    }

    void RandomControl() {
        if (Time.time - _lastUpdate > 2)
        {
            _leftTorque = UnityEngine.Random.Range(-FULL_TORQUE, FULL_TORQUE);
            _rightTorque = UnityEngine.Random.Range(-FULL_TORQUE, FULL_TORQUE);
            _lastUpdate = Time.time;
        }
    }

    void Start()
    {
        if (Port > 0)
        {
            _socket = new UdpClient(Port);
            Debug.Log($"Listening for UDP packets on port: {Port}");
            ListenForUDP();
        }
    }

    void Update()
    {
        if (Control == ControlDevices.ArrowKeys)
        {
            ListenArrowKeys();
        }
        if (Control == ControlDevices.WasdKeys)
        {
            ListenWasdKeys();
        }
        if (Control == ControlDevices.Random) {
            RandomControl();
        }

        foreach (WheelCollider wheelCollider in leftWheels)
        {
            wheelCollider.motorTorque = _leftTorque;
        }
        foreach (WheelCollider wheelCollider in rightWheels)
        {
            wheelCollider.motorTorque = _rightTorque;
        }
    }
}
