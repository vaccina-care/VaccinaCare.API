﻿namespace VaccinaCare.Application.Interface.Common;

public interface ILoggerService
{
    void Success(string msg);

    void Error(string msg);

    void Warn(string msg);

    void Info(string msg);
}