﻿using GpsUtil.Location;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;
using TourGuide.Utilities;
using TripPricer;

namespace TourGuide.Services;

public class TourGuideService : ITourGuideService
{
    private readonly ILogger _logger;
    private readonly IGpsUtil _gpsUtil;
    private readonly IRewardCentral _rewardCentral;
    private readonly IRewardsService _rewardsService;
    private readonly TripPricer.TripPricer _tripPricer;
    public Tracker Tracker { get; private set; }
    private readonly Dictionary<string, User> _internalUserMap = new();
    private const string TripPricerApiKey = "test-server-api-key";
    private bool _testMode = true;


    public TourGuideService(ILogger<TourGuideService> logger, IGpsUtil gpsUtil, IRewardsService rewardsService, ILoggerFactory loggerFactory, IRewardCentral rewardCentral)
    {
        _logger = logger;
        _tripPricer = new();
        _gpsUtil = gpsUtil;
        _rewardsService = rewardsService;
        _rewardCentral = rewardCentral;

        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        if (_testMode)
        {
            _logger.LogInformation("TestMode enabled");
            _logger.LogDebug("Initializing users");
            InitializeInternalUsers();
            _logger.LogDebug("Finished initializing users");
        }

        var trackerLogger = loggerFactory.CreateLogger<Tracker>();

        Tracker = new Tracker(this, trackerLogger);
        AddShutDownHook();

    }

    public List<UserReward> GetUserRewards(User user)
    {
        return user.UserRewards;
    }

    public VisitedLocation GetUserLocation(User user)
    {
        return user.VisitedLocations.Any() ? user.GetLastVisitedLocation() : TrackUserLocation(user);
    }

    public User GetUser(string userName)
    {
        return _internalUserMap.ContainsKey(userName) ? _internalUserMap[userName] : null;
    }

    public List<User> GetAllUsers()
    {
        return _internalUserMap.Values.ToList();
    }

    public void AddUser(User user)
    {
        if (!_internalUserMap.ContainsKey(user.UserName))
        {
            _internalUserMap.Add(user.UserName, user);
        }
    }

    public List<Provider> GetTripDeals(User user)
    {
        int cumulativeRewardPoints = user.UserRewards.Sum(i => i.RewardPoints);
        List<Provider> providers = _tripPricer.GetPrice(TripPricerApiKey, user.UserId,
            user.UserPreferences.NumberOfAdults, user.UserPreferences.NumberOfChildren,
            user.UserPreferences.TripDuration, cumulativeRewardPoints);
        user.TripDeals = providers;
        return providers;
    }

    public VisitedLocation TrackUserLocation(User user)
    {

        VisitedLocation visitedLocation = _gpsUtil.GetUserLocation(user.UserId);
        user.AddToVisitedLocations(visitedLocation);
        _rewardsService.CalculateRewards(user);

        return visitedLocation;
    }

    public List<Object> GetNearByAttractions(VisitedLocation visitedLocation)
    {
        List<Attraction> nearbyAttractions = new ();
        List<(Attraction,double)> ListAllAttractionsWithDistance = new ();
        foreach (var attraction in _gpsUtil.GetAttractions())
        {
            double distance = _rewardsService.GetDistance(attraction, visitedLocation.Location);
            ListAllAttractionsWithDistance.Add((attraction, distance));
        }
        List<(Attraction, double)> ListAllAttractionsWithDistancesort = ListAllAttractionsWithDistance.OrderBy(x => x.Item2).ToList();

        nearbyAttractions.AddRange(new List<Attraction>
        { 
            ListAllAttractionsWithDistancesort[0].Item1,
            ListAllAttractionsWithDistancesort[1].Item1,
            ListAllAttractionsWithDistancesort[2].Item1,
            ListAllAttractionsWithDistancesort[3].Item1,
            ListAllAttractionsWithDistancesort[4].Item1 
        });

        var newNearbyAttraction = nearbyAttractions
            .Select(x => new
                {
            attractionName= x.AttractionName,
            attractionLatitude = x.Latitude,
            attractionLongitude = x.Longitude,
            userLatitude = visitedLocation.Location.Latitude,
            userLongitude = visitedLocation.Location.Longitude,
            distanceAttractionUser = _rewardsService.GetDistance(x, visitedLocation.Location),
            rewardPoints = _rewardCentral.GetAttractionRewardPoints(x.AttractionId, visitedLocation.UserId)
            })
                .ToList();
        return newNearbyAttraction.Cast<object>().ToList();
    }

    private void AddShutDownHook()
    {
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => Tracker.StopTracking();
    }

    /**********************************************************************************
    * 
    * Methods Below: For Internal Testing
    * 
    **********************************************************************************/

    private void InitializeInternalUsers()
    {
        for (int i = 0; i < InternalTestHelper.GetInternalUserNumber(); i++)
        {
            var userName = $"internalUser{i}";
            var user = new User(Guid.NewGuid(), userName, "000", $"{userName}@tourGuide.com");
            GenerateUserLocationHistory(user);
            _internalUserMap.Add(userName, user);
        }

        _logger.LogDebug($"Created {InternalTestHelper.GetInternalUserNumber()} internal test users.");
    }

    private void GenerateUserLocationHistory(User user)
    {
        for (int i = 0; i < 3; i++)
        {
            var visitedLocation = new VisitedLocation(user.UserId, new Locations(GenerateRandomLatitude(), GenerateRandomLongitude()), GetRandomTime());
            user.AddToVisitedLocations(visitedLocation);
        }
    }

    private static readonly Random random = new Random();

    private double GenerateRandomLongitude()
    {
        return new Random().NextDouble() * (180 - (-180)) + (-180);
    }

    private double GenerateRandomLatitude()
    {
        return new Random().NextDouble() * (90 - (-90)) + (-90);
    }

    private DateTime GetRandomTime()
    {
        return DateTime.UtcNow.AddDays(-new Random().Next(30));
    }
}
