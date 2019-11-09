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
    public class PlayHub : Hub
    {
        private readonly JeopicodusContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public PlayHub(JeopicodusContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public void JoinHub(string playerId)
        {
            Console.WriteLine("Joining Hub");
            if (_userManager.Users.FirstOrDefault(player => player.ConnectionId == Context.ConnectionId) == null)
            {
                var currentPlayer = _userManager.Users.FirstOrDefault(player => player.Id == playerId);
                currentPlayer.ConnectionId = Context.ConnectionId;
                _db.Entry(currentPlayer).State = EntityState.Modified;
                _db.SaveChanges();
            }
        }

        public void QuestionClicked(string data)
        {
            var splitData = data?.Split(new char[] {
            '#' }, StringSplitOptions.None);
            string questionId = splitData[0];
            string teamName = splitData[1];
            Console.WriteLine("TEAMNAME " + teamName);
            var thisGame = _db.Games.Include(g => g.Teams).FirstOrDefault(game => game.Teams.Any(t => t.TeamName == teamName));

            if (thisGame == null)
            {
                Console.WriteLine("No game");
                return; // no game found with this credentials
            }

            var activeTeam = thisGame.Teams.FirstOrDefault(team => team.TeamName == teamName);

            if (!activeTeam.IsTurn)
            {
                Console.WriteLine("Not team's turn");
                return;  // not this team's turn, dont do anything
            }
            else
            {
                activeTeam.IsTurn = false;
                _db.Entry(thisGame).State = EntityState.Modified;
                _db.SaveChanges();
            }

            Clients.All.SendAsync("showQuestion", questionId);
        }

        public void AnswerQuestion(string data)
        {
            var splitData = data?.Split(new char[] {
            '#' }, StringSplitOptions.None);
            string questionId = splitData[0];
            string teamName = splitData[1];
            var thisGame = _db.Games.Include(g => g.Teams).FirstOrDefault(game => game.Teams.Any(t => t.TeamName == teamName));

            if (thisGame == null)
            {
                Console.WriteLine("No game");
                return; // no game found with this credentials
            }

            var activeTeam = thisGame.Teams.FirstOrDefault(team => team.TeamName == teamName);

            if (thisGame.Teams.Where(team => team.IsTurn == true).ToList().Count > 0)
            {
                Console.WriteLine("Already triggered");
                return;  // not this team's turn, dont do anything
            }

            activeTeam.IsTurn = true;
            _db.Entry(thisGame).State = EntityState.Modified;
            _db.SaveChanges();

            Console.WriteLine("active team" + activeTeam.TeamName);
            Clients.All.SendAsync("buzzedIn", activeTeam.TeamName);
        }

        public void CorrectAnswer(string data)
        {

            var splitData = data?.Split(new char[] {
            '#' }, StringSplitOptions.None);
            string teamId = splitData[0];
            string valueToAdd = splitData[1];

            var game = _db.Games.Include(g => g.Teams).FirstOrDefault(g => g.Teams.Any(t => t.TeamId.ToString() == teamId));

            if (game == null)
            {
                Console.WriteLine("No Game Found");
                return; // no game found
            }
            var thisTeam = game.Teams.FirstOrDefault(t => t.TeamId.ToString() == teamId);
            if (thisTeam == null || thisTeam.IsTurn == false)
            {
                Console.WriteLine("Not Allowed to Submit Answer");
                return;
            }
            List<Team> TeamList = new List<Team> { };
            foreach (Team team in game.Teams)
            {
                TeamList.Add(team);
            }
            int teamIndex = TeamList.IndexOf(game.Teams.FirstOrDefault(t => t.TeamId.ToString() == teamId));
            thisTeam.IsTurn = false;
            if (teamIndex == 0)
            {
                game.ScoreTeam1 += Int32.Parse(valueToAdd);
            }
            else
            {
                game.ScoreTeam2 += Int32.Parse(valueToAdd);
            }
            _db.Entry(thisTeam).State = EntityState.Modified;
            _db.Entry(game).State = EntityState.Modified;
            _db.SaveChanges();

            Clients.All.SendAsync("updateScores", new UpdateScoresModel() { Game = game, ActiveTeamId = teamId });

        }

        public void WrongAnswer(string data)
        {
            var splitData = data?.Split(new char[] {
            '#' }, StringSplitOptions.None);
            string teamId = splitData[0];
            string valueToLose = splitData[1];

            var game = _db.Games.Include(g => g.Teams).FirstOrDefault(g => g.Teams.Any(t => t.TeamId.ToString() == teamId));

            if (game == null)
            {
                Console.WriteLine("No Game Found");
                return; // no game found
            }
            var thisTeam = game.Teams.FirstOrDefault(t => t.TeamId.ToString() == teamId);
            if (thisTeam == null || thisTeam.IsTurn == false)
            {
                Console.WriteLine("Not Allowed to Submit Answer");
                return;
            }
            List<Team> TeamList = new List<Team> { };
            foreach (Team team in game.Teams)
            {
                TeamList.Add(team);
            }
            int teamIndex = TeamList.IndexOf(game.Teams.FirstOrDefault(t => t.TeamId.ToString() == teamId));
            thisTeam.IsTurn = false;
            string nextTeamId = "";
            if (teamIndex == 0)
            {
                game.ScoreTeam1 -= Int32.Parse(valueToLose);
                TeamList[1].IsTurn = true;
                nextTeamId = TeamList[1].TeamId.ToString();
            }
            else
            {
                game.ScoreTeam2 -= Int32.Parse(valueToLose);
                TeamList[0].IsTurn = true;
                nextTeamId = TeamList[0].TeamId.ToString();
            }
            _db.Entry(TeamList[0]).State = EntityState.Modified;
            _db.Entry(TeamList[1]).State = EntityState.Modified;
            _db.Entry(game).State = EntityState.Modified;
            _db.SaveChanges();

            Clients.All.SendAsync("otherTeamToTry", new UpdateScoresModel() { Game = game, ActiveTeamId = nextTeamId });
        }

        public void UpdateTeamTurn(string teamId)
        {
            var game = _db.Games.Include(g => g.Teams).FirstOrDefault(g => g.Teams.Any(t => t.TeamId.ToString() == teamId));

            if (game == null)
            {
                Console.WriteLine("No Game Found");
                return; // no game found
            }
            var thisTeam = game.Teams.FirstOrDefault(t => t.TeamId.ToString() == teamId);
            if (thisTeam == null)
            {
                Console.WriteLine("Team Not Found");
                return;
            }
            thisTeam.IsTurn = true;
            _db.Entry(thisTeam).State = EntityState.Modified;
            _db.SaveChanges();
        }

        public void WrongAnswerNextQuestion(string data)
        {
            var splitData = data?.Split(new char[] {
            '#' }, StringSplitOptions.None);
            string teamId = splitData[0];
            string valueToLose = splitData[1];

            var game = _db.Games.Include(g => g.Teams).FirstOrDefault(g => g.Teams.Any(t => t.TeamId.ToString() == teamId));

            if (game == null)
            {
                Console.WriteLine("No Game Found");
                return; // no game found
            }
            var thisTeam = game.Teams.FirstOrDefault(t => t.TeamId.ToString() == teamId);
            if (thisTeam == null || thisTeam.IsTurn == false)
            {
                Console.WriteLine("Not Allowed to Submit Answer");
                return;
            }
            List<Team> TeamList = new List<Team> { };
            foreach (Team team in game.Teams)
            {
                TeamList.Add(team);
            }
            int teamIndex = TeamList.IndexOf(game.Teams.FirstOrDefault(t => t.TeamId.ToString() == teamId));
            thisTeam.IsTurn = false;
            string nextTeamId = "";
            if (teamIndex == 0)
            {
                game.ScoreTeam1 -= Int32.Parse(valueToLose);
                TeamList[1].IsTurn = false;
                nextTeamId = TeamList[1].TeamId.ToString();
            }
            else
            {
                game.ScoreTeam2 -= Int32.Parse(valueToLose);
                TeamList[0].IsTurn = false;
                nextTeamId = TeamList[0].TeamId.ToString();
            }
            _db.Entry(TeamList[0]).State = EntityState.Modified;
            _db.Entry(TeamList[1]).State = EntityState.Modified;
            _db.Entry(game).State = EntityState.Modified;
            _db.SaveChanges();

            Clients.All.SendAsync("updateScores", new UpdateScoresModel() { Game = game, ActiveTeamId = nextTeamId });
        }

        public void NextQuestion(string teamId)
        {
            Clients.All.SendAsync("closeModal");
        }
    }
}