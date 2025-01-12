﻿namespace VaccinaCare.Repository.Interfaces
{
    public interface IUnitOfWork
    {
        //    IGenericRepository<Area> AreaGenericRepository { get; }
        //    IGenericRepository<Category> CategoryGenericRepository { get; }

        Task<int> SaveChangesAsync();
    }

}