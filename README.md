# _Jeopicodus_

#### _Play Jeopardy to learn your epicodus curriculum. {11/6/2019}_

#### By _**Christine Frank, Mike McShane, Chris Burge, and Devin Cooley**_

## Description

_The purpose of this application is to allow two teams to compete at the game of jeopardy, from two different monitors, simultaneously. The game board is populated with questions taken directly from the epicodus curriculum, making learning easy and fun!_  

## Specifications
* #### Authentication
| Spec                      |Input          | Output |
|:---------------------------|:-------------|:------|
|Allows a user to create a profile.| Name: John, email: email@a.com, etc| New user John|
|Allows a user to login| Name: John, PassWord: ******| Welcome John!|
|Only allows user access to game if logged in|`Not Logged In`| `Reroute to Login`| 

* #### Game Initialization
| Spec                      |Input          | Output |
|:---------------------------|:-------------|:------|
|Allows the user to create a new game |`New Game`| _Creates new game and adds user to game._|
|Allows user to Join a Game that has already been created_|`Join Game`|_Adds user to game._|
|Initializes the game with all users assigned to it| `Game Start`| _LETS PLAY JEOPARDY!_ | 

* #### Game Play
| Spec                      |Input          | Output |
|:---------------------------|:-------------|:------|
|Displays the game board for all teams|`Game Starts`| `All users on all screens see gameboard`|
|Populates the game board with questions pulled from Jeopicodus API| `Game Starts`| `Game board populates with unique questions from api`|
|Disables clicking of categories for one team, creating turns| `Team one Turn`|`Team two may not click`|
|Allows the team up to select a question|`Clicks Category`|`Question shows`|
|Allows any team to "buzz in" by clicking the question|`Team 1 "buzz in"`|`Team 1's screen shows answer form`|
|Awards Team points for a correct answer|`CORRECT ANSWER!`|`Team 1 score up ## points`|
|If a team answers incorrectly, question is clickable by other teams| `WRONG ANSWER`| `Other teams able to "buzz in"/click on question|
|After a question is answered or goes unanswered, Category selection reopens for other team| `CORRECT ANSWER` | `Other team now able to select question`|
|When all questions have been played upon, game ends, alerting victor| Team 1 Score: 5000, Team 2 Score: 5400| `TEAM 2 WINS`|
|When game ends, GameBoard is hidden and both teams are returned to Game selection.| `TEAM 2 WINS`| _Team Selection and Game Creation screen displays._|



## Setup/Installation Requirements
_Simply run this application in your browser. Create a user, log in, and join or create a game to start playing. Communicate with a friend to join in the same game from different clients and play together live._

## Game Location  
* https://playjeopicodus.azurewebsites.net

## Known Bugs

_There are no known bugs at this time._

## Support and contact details

_Send any questions or comments to:
Christine Frank at christine.braun13@gmail.com  
Chris Burge at topher.burge@gmail.com
Mike McShane at  mmchane10@gmail.com 
Devin Cooley at dcooley1350@gmail.com._

## Technologies Used

_This is a .NET Core MVC application written primarily in C#. JavaScript, HTML and CSS were implemented alongside C#. Databasing was accomplished with Mysql, assisted by Entity Framework. Authorization was handled using Identity for .NET Core. This app features live data rendering with use of SignalR for .NET. This application and the accompanying database are hosted with Microsoft Azure Cloud Services. View this application in your browser or another client.

### License

*This software is licensed under the MIT license.*

Copyright (c) 2019 **_Devin Cooley, Christine Frank, Chris Burge, Mike McShane_**