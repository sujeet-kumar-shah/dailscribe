using System;

namespace DayScribe.Services;

public interface IActivityTracker
{
    bool IsRunning { get; }
    void Start();
    void Stop();
    event Action<string, string>? OnActivityLogged;
}
