using GpsUtil.Location;
using Microsoft.AspNetCore.Mvc;
using TourGuide.Services.Interfaces;
using TourGuide.Users;
using TripPricer;

namespace TourGuide.Controllers;

[ApiController]
[Route("[controller]")]
public class TourGuideController : ControllerBase
{
    private readonly ITourGuideService _tourGuideService;

    public TourGuideController(ITourGuideService tourGuideService)
    {
        _tourGuideService = tourGuideService;

    }

    [HttpGet("getLocation")]
    public ActionResult<VisitedLocation> GetLocation([FromQuery] string userName)
    {
        var location = _tourGuideService.GetUserLocation(GetUser(userName));
        return Ok(location);
    }

    // TODO: Change this method to no longer return a List of Attractions.
    // Instead: Get the closest five tourist attractions to the user - no matter how far away they are.
    // Return a new JSON object that contains:
    // Name of Tourist attraction, 
    // Tourist attractions lat/long, 
    // The user's location lat/long, 
    // The distance in miles between the user's location and each of the attractions.
    // The reward points for visiting each Attraction.
    //    Note: Attraction reward points can be gathered from RewardsCentral
/*// TODO : modifiez cette méthode pour ne plus renvoyer de liste d'attractions.
// Au lieu de cela : obtenez les cinq attractions touristiques les plus proches de l'utilisateur, quelle que soit leur distance.
// Renvoie un nouvel objet JSON qui contient :
// Nom de l'attraction touristique,
// Attractions touristiques latitude/longitude,
// Localisation de l'utilisateur lat/long,
// La distance en miles entre l'emplacement de l'utilisateur et chacune des attractions.
// Les points de récompense pour la visite de chaque attraction.
// Remarque : les points de récompense d'attraction peuvent être collectés depuis RewardsCentral*/
[HttpGet("getNearbyAttractions")]
public async Task<ActionResult<List<Object>>> GetNearbyAttractions([FromQuery] string userName)
{
    var visitedLocation = await _tourGuideService.GetUserLocation(GetUser(userName));
    var attractions = await _tourGuideService.GetNearByAttractions(visitedLocation);
    return Ok(attractions);
}

[HttpGet("getRewards")]
public ActionResult<List<UserReward>> GetRewards([FromQuery] string userName)
{
    var rewards = _tourGuideService.GetUserRewards(GetUser(userName));
    return Ok(rewards);
}

[HttpGet("getTripDeals")]
public ActionResult<List<Provider>> GetTripDeals([FromQuery] string userName)
{
    var deals = _tourGuideService.GetTripDeals(GetUser(userName));
    return Ok(deals);
}

private User GetUser(string userName)
{
    return _tourGuideService.GetUser(userName);
}
}
