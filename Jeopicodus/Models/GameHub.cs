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

        public Task CreateGame(string data)
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
            Console.WriteLine(newGame.GameId.ToString());

            return Groups.AddToGroupAsync(Context.ConnectionId, newGame.GameId.ToString());
            // var player = players?.FirstOrDefault(x =>
            // x.ConnectionId == Context.ConnectionId);
        }

        public Task CreateTeam(string data)
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
            Console.WriteLine(gameId);

            return Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        }

        public Task JoinTeam(string data)
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
            Console.WriteLine(gameId);
            return Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        }

        public void GameAdded()
        {
            List<Game> gameData = _db.Games.Include(g => g.Teams).ToList();
            Clients.All.SendAsync("ShowGames", gameData);
        }

        public async Task StartGame(string gameId)
        {
            Console.WriteLine(gameId);
            await Clients.Group(gameId).SendAsync("goToGame", gameId);
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