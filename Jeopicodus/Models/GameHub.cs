using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Jeopicodus.Entities;

namespace Jeopicodus.Models
{
    public class GameHub : Hub
    {
        private readonly JeopicodusContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public GameHub(JeopicodusContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }
        
        public async Task Send(string teamName, string message)
        {
            await Clients.All.SendAsync("Send", teamName, message);
        }

        public void JoinHub(string playerId)
        {
            Console.WriteLine("Joining Hub");
            if(_userManager.Users.FirstOrDefault(player => player.ConnectionId == Context.ConnectionId) == null)
            {
                var currentPlayer = _userManager.Users.FirstOrDefault(player => player.Id == playerId);
                currentPlayer.ConnectionId = Context.ConnectionId;
                _db.Entry(currentPlayer).State = EntityState.Modified;
                _db.SaveChanges();
            }
        }

        public void CreateGame(string data)
        {
            var splitData = data?.Split(new char[] {
            '#' }, StringSplitOptions.None);
            string team1Name = splitData[0];
            string playerId = splitData[1];
            var currentUser = _userManager.Users.FirstOrDefault(p => p.Id == playerId);
            Game newGame = new Game();
            _db.Games.Add(newGame);
            _db.SaveChanges();

            Team newTeam = new Team(){ TeamName = team1Name };
            newTeam.GameId = newGame.GameId;
            _db.Teams.Add(newTeam);
            _db.SaveChanges();

            _db.TeamMembers.Add(new TeamMember(){ TeamId = newTeam.TeamId, ApplicationUserId = currentUser.Id });
            _db.SaveChanges();
            // var player = players?.FirstOrDefault(x =>
            // x.ConnectionId == Context.ConnectionId);
        }

        public void CreateTeam(string data)
        {
            var splitData = data?.Split(new char[] {
            '#' }, StringSplitOptions.None);
            string gameId = splitData[0];
            string team2Name = splitData[1];
            string playerId = splitData[2];
            var currentUser = _userManager.Users.FirstOrDefault(p => p.Id == playerId);
            Team newTeam = new Team(){TeamName = team2Name};
            var currentGame = _db.Games.FirstOrDefault(g => g.GameId.ToString() == gameId);
            newTeam.GameId = currentGame.GameId;
            _db.Teams.Add(newTeam);
            _db.SaveChanges();
            _db.TeamMembers.Add(new TeamMember() { TeamId = newTeam.TeamId, ApplicationUserId = currentUser.Id });
            _db.Entry(currentGame).State = EntityState.Modified;
            _db.SaveChanges();
        }

        public void JoinTeam(string data)
        {
            var splitData = data?.Split(new char[] {
            '#' }, StringSplitOptions.None);
            string gameId = splitData[0];
            string teamId = splitData[1];
            string playerId = splitData[2];
            var currentUser = _userManager.Users.FirstOrDefault(p => p.Id == playerId);
            var currentGame = _db.Games.FirstOrDefault(g => g.GameId.ToString() == gameId);
            _db.TeamMembers.Add(new TeamMember() { TeamId = Int32.Parse(teamId), ApplicationUserId = currentUser.Id });
            _db.Entry(currentGame).State = EntityState.Modified;
            _db.SaveChanges();
        }

        public void GameAdded()
        {
            List<Game> gameData = _db.Games.Include(g => g.Teams).ToList();
            Clients.All.SendAsync("ShowGames", gameData);
        }

    }
}