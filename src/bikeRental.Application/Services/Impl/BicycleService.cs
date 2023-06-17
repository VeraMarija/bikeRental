﻿using AutoMapper;
using bikeRental.Core.Entities;
using bikeRental.Application.Models.Bicycle;
using bikeRental.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bikeRental.Application.Models.Station;
using bikeRental.DataAccess.Repositories.Impl;
using static System.Collections.Specialized.BitVector32;

namespace bikeRental.Application.Services.Impl;
public class BicycleService : IBicycleService
{
    private readonly IMapper _mapper;
    private readonly IBicycleRepository<Bicycle> _bicycleRepository;
    private readonly IStationService _stationService;
    public BicycleService(IBicycleRepository<Bicycle> bicycleRepository, IMapper mapper, IStationService stationService)
    {
        _bicycleRepository = bicycleRepository;
        _mapper = mapper;
        _stationService = stationService;
    }
    public async Task<IEnumerable<BicycleModel>> GetAllAsync()
    {
        var bicycles = await _bicycleRepository.GetAllAsync();

        foreach (var bicycle in bicycles)
        {
            var station = await _stationService.GetByIdAsync(bicycle.Station.Id);

            bicycle.Station = _mapper.Map<Station>(station);
        }
        var bicyclesModels = _mapper.Map<IEnumerable<BicycleModel>>(bicycles);
        return bicyclesModels;

    }

    public List<string> getFieldNames()
    {
        Bicycle Bicycle = new Bicycle();
        return Bicycle.GetType().GetProperties().Where(x => x.Name != "Id").Select(x => x.Name).ToList();
    }

    public async Task Delete(Guid Id, Guid stationId)
    {
        var bicycle = await _bicycleRepository.GetByIdAsync(Id);
        var station = await _stationService.GetByIdAsync(stationId);
        if(bicycle.Type == BikeType.Acoustic)
        {
            station.NumberOfBikes--;
        }
        else
        {
            station.NumberOfElectricBikes--;
        }
        await _stationService.UpdateAsync(station);
        await _bicycleRepository.DeleteAsync(Id);

    }

    public async Task<IEnumerable<BicycleModel>> GetByStation(Guid StationId)
    {
        var bicycles = await _bicycleRepository.GetByStation(StationId);
        var bicyclesModel = _mapper.Map<IEnumerable<BicycleModel>>(bicycles);
        foreach (var bicycle in bicyclesModel)
        {
            bicycle.Station = await _stationService.GetByIdAsync(StationId);
        }
        return bicyclesModel;
    }

    public async Task<BicycleModel> GetByIdAsync(Guid? id, Guid stationId)
    {
        var response = await _bicycleRepository.GetByIdAsync(id);
        var bicycleModel = _mapper.Map<BicycleModel>(response);
        bicycleModel.Station = await _stationService.GetByIdAsync(stationId);
        return bicycleModel;
    }

    public async Task<BicycleModel> GetByIdAsync(Guid? id)
    {
        var response = await _bicycleRepository.GetByIdAsync(id);
        var bicycleModel = _mapper.Map<BicycleModel>(response);
        return bicycleModel;
    }

    public IEnumerable<BicycleModel> SearchSelection(IEnumerable<BicycleModel> bicycles, string searchString)
    {
        IEnumerable<BicycleModel> stationsSearched = bicycles.ToList();

        if (!String.IsNullOrEmpty(searchString))
        {
            var searchStrTrim = searchString.Trim();
            stationsSearched = bicycles.Where(t => t.Description.Contains(searchStrTrim));
        }
        return stationsSearched;
    }

    public async Task<BicycleModel> AddAsync(BicycleModel bicycleModel, Guid stationId)
    {
        var bicycle = _mapper.Map<Bicycle>(bicycleModel);
        //var station = await _stationService.GetByIdAsync(stationId);
        //bicycle.Station = _mapper.Map<Station>(station);
        //System.Diagnostics.Debug.WriteLine(bicycle.Station.Id + "Kurcina na kv");

        bicycle = await _bicycleRepository.AddAsync(bicycle, stationId);
        
        return _mapper.Map<BicycleModel>(bicycle);
    }

    public async Task UpdateAsync(BicycleModel bicycleModel)
    {
        var bicycleNew = _mapper.Map<Bicycle>(bicycleModel);
        var bicycleOld = await _bicycleRepository.GetByIdAsync(bicycleModel.Id);

        bicycleModel.Station = await _stationService.GetByIdAsync(bicycleModel.Station.Id);

        if (bicycleModel.Type.ToString() == "Electric" && bicycleOld.Type.ToString() != "Electric")
        {
            bicycleModel.Station.NumberOfBikes--;
            bicycleModel.Station.NumberOfElectricBikes++;
        }
        else if (bicycleModel.Type.ToString() == "Acoustic" && bicycleOld.Type.ToString() != "Acoustic")
        {
            bicycleModel.Station.NumberOfElectricBikes--;
            bicycleModel.Station.NumberOfBikes++;
        }

        await _stationService.UpdateAsync(bicycleModel.Station);
        await _bicycleRepository.UpdateAsync(bicycleNew, bicycleModel.Station.Id);
    }


}

