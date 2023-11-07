using Kulturformidling_api.Data;
using Kulturformidling_api.Data.Model;
using Kulturformidling_api.dtoModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Type = Kulturformidling_api.Data.Model.Type;

namespace Kulturformidling_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Administrator")]
    [Produces("application/json")]
    public class CRUDController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;
        public CRUDController(DataContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("ArtByType")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ArtDto>>> getArtList(Type request)
        {
            var artList = _context.Art.Where(a => a.Type.Id == request.Id).ToList();

            var returnList = new List<ArtDto>();

            foreach (var art in artList)
            {
                var artDto = new ArtDto()
                {
                    ArtId = art.Id,
                    ArtistId = art.Artist.Id,
                    ArtName = art.Name,
                    Author = art.Author,
                    Description = art.Description,
                    From = art.From,
                    To = art.To,
                    TypeId = art.Type.Id
                };

                returnList.Add(artDto);
            }
            
            return Ok(returnList);
        }

        [HttpPost("Art")]
        public async Task<ActionResult<Art>> addArt(ArtDto request)
        {
            var art = new Art();

            var artist = _context.Users.FirstOrDefault(u => u.Id == request.ArtistId);

            var type = _context.Types.FirstOrDefault(t => t.Id == request.TypeId);

            art.Name = request.ArtName;
            art.Description = request.Description;
            art.From = request.From;
            art.To = request.To;
            art.Type = type;
            art.Artist = artist;
            art.Author = User.Identity.Name;
            
            _context.Art.Add(art);
            _context.SaveChanges();

            return Ok(art);
        }

        [HttpPut("Art")]
        public async Task<ActionResult<Art>> editArt(ArtDto request)
        {
            var art = _context.Art.FirstOrDefault(a => a.Id == request.ArtId);

            var artist = _context.Users.FirstOrDefault(u => u.Id == request.ArtistId);

            var type = _context.Types.FirstOrDefault(t => t.Id == request.TypeId);

            art.Name = request.ArtName;
            art.Description = request.Description;
            art.From = request.From;
            art.To = request.To;
            art.Type = type;
            art.Artist = artist;

            _context.SaveChanges();

            return Ok(art);
        }

        [HttpDelete("Art")]
        public async Task<ActionResult<Art>> deleteArt(ArtDto request)
        {
            var art = _context.Art.FirstOrDefault(a => a.Id == request.ArtId);

            _context.Art.Remove(art);

            _context.SaveChanges();

            return Ok(art);
        }

        [HttpGet("Types")]
        [AllowAnonymous]
        public async Task<ActionResult<List<Type>>> getTypes()
        {
            var types = _context.Types.ToList();

            return Ok(types);
        }

        [HttpPost("Type")]
        public async Task<ActionResult<Type>> addType(Type request)
        {
            var type = new Type();

            type.Name = request.Name;
            _context.Add(type);
            _context.SaveChanges();

            return Ok(type);
        }

    }
}
