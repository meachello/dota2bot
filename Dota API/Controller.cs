using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace Dota_API;
[ApiController]
[Route("[controller]")]
public class Controller
{
    private DB database = new DB();
    private string? _telegramName;
    private int _dotaId;
    
    [HttpGet("/match/{match:long}/{id:long}")]
    public async Task<string> GetMatch(long match, long id)
    {
        return await GetMatchID(match, id);
    }

    [HttpGet("/id/{id:long}")]
    public async Task<string> GetAccount(long id)
    {
        return await Get32Id(id);
    }
    
    [HttpGet("/id64/{id:long}")]
    public async Task<string> GetAccount64(long id)
    {
        id <<= 32;
        id >>= 32;
        return await Get32Id(id);
    }

    public class DotaUser
    {
        public long DotaID { get; set; }
        public string Username { get; set; }
    }
    
    [HttpPost("/add_id")]
    public void Post([FromBody]DotaUser dota)
    {
        MySqlCommand insertID = new MySqlCommand($"INSERT INTO account_id.id (id, telegram_id) VALUES ({dota.DotaID}, '{dota.Username}')", database.getConnection());
        _telegramName = dota.Username;
        database.OpenConnection();
        insertID.ExecuteNonQuery();
    }

    [HttpGet("/get_id/{telegram_name}")]
    public async Task<string> GetID(string telegram_name)
    {
        MySqlCommand selectID = new MySqlCommand($"SELECT id FROM account_id.id WHERE telegram_id = '{telegram_name}'", database.getConnection());
        database.OpenConnection();
        var idJson = selectID.ExecuteReader();
        string id="";
        if (idJson.Read())
        {
            id = idJson.GetString(idJson.GetOrdinal("id"));
        }

        return id;
    }
    
    [HttpDelete("/remove_id/{telegram_name}")]
    public void Delete(string telegram_name)
    {
        MySqlCommand removeID = new MySqlCommand($"DELETE FROM account_id.id WHERE telegram_id ='{telegram_name}'", database.getConnection());
        database.OpenConnection();
        removeID.ExecuteNonQuery();
    }

    [HttpGet("/heroes/{id:long}")]
    public async Task<string> GetHeroes(long id)
    {
        var request = await new HttpClient().GetAsync($"https://api.opendota.com/api/players/{id}/heroes");
        var response = await request.Content.ReadAsStringAsync();
        var unsortedBody = JsonObject.Parse(response);
        int firstSign = 0;
        int firstGames = 0;
        int secondSign = 0;
        int secondGames = 0;
        int thirdSign = 0;
        int thirdGames = 0;
        foreach (var hero in unsortedBody.AsArray())
        {
            var hero_id = Convert.ToInt32((string)hero["hero_id"]);
            double winrate = (double)hero["win"] / (double)hero["games"];
            if (winrate>=0.5 && (int)hero["games"]>firstGames)
            {
                firstSign = hero_id;
                firstGames = (int)hero["games"];
            }

            if (winrate>=0.5 && (int)hero["games"]>secondGames && hero_id!=firstSign)
            {
                secondSign = hero_id;
                secondGames = (int)hero["games"];
            }

            if (winrate>=0.5 && (int)hero["games"]>thirdGames && hero_id!=firstSign && hero_id!=secondSign)
            {
                thirdSign = hero_id;
                thirdGames = (int)hero["games"];
            }
        }
        var request1 = await new HttpClient().GetAsync($"http://dotabotapi.azurewebsites.net/heroes/{firstSign}/matchup");
        var response1 = await request1.Content.ReadAsStringAsync();
        var request2 = await new HttpClient().GetAsync($"http://dotabotapi.azurewebsites.net/heroes/{secondSign}/matchup");
        var response2 = await request2.Content.ReadAsStringAsync();
        var request3 = await new HttpClient().GetAsync($"http://dotabotapi.azurewebsites.net/heroes/{thirdSign}/matchup");
        var response3 = await request3.Content.ReadAsStringAsync();

        var firstBan = response1.Split("+");
        var firstSignBan1 = Convert.ToInt32(firstBan[0]);
        var firstSignBan2 = Convert.ToInt32(firstBan[1]);
        var firstSignBan3 = Convert.ToInt32(firstBan[2]);
        
        var secondBan = response2.Split("+");
        var secondSignBan1 = Convert.ToInt32(secondBan[0]);
        var secondSignBan2 = Convert.ToInt32(secondBan[1]);
        var secondSignBan3 = Convert.ToInt32(secondBan[2]);
        
        var thirdBan = response3.Split("+");
        var thirdSignBan1 = Convert.ToInt32(thirdBan[0]);
        var thirdSignBan2 = Convert.ToInt32(thirdBan[1]);
        var thirdSignBan3 = Convert.ToInt32(thirdBan[2]);
        return $"There are your signature heroes and the best counter-picks of them:\n\n" +
               $"First hero:  {Heroes.GetName(firstSign)}\nBest counter-picks, that you need to ban:  {Heroes.GetName(firstSignBan1)}, {Heroes.GetName(firstSignBan2)}, {Heroes.GetName(firstSignBan3)}\n\n" +
               $"Second hero:  {Heroes.GetName(secondSign)}\nBest counter-picks, that you need to ban:  {Heroes.GetName(secondSignBan1)}, {Heroes.GetName(secondSignBan2)}, {Heroes.GetName(secondSignBan3)}\n\n" +
               $"Third hero:  {Heroes.GetName(thirdSign)}\nBest counter-picks, that you need to ban:  {Heroes.GetName(thirdSignBan1)}, {Heroes.GetName(thirdSignBan2)}, {Heroes.GetName(thirdSignBan3)}\n\n";
    }

    [HttpGet("/heroes/{heroID:int}/ctpick")]
    public async Task<string> GetCounterPick(string heroID)
    {
            var requestCT = await new HttpClient().GetAsync($"http://dotabotapi.azurewebsites.net/heroes/{heroID}/matchup");
            var responseCT = await requestCT.Content.ReadAsStringAsync();
            var counterpicks = responseCT.Split("+");
            var counterpick1 = Convert.ToInt32(counterpicks[0]);
            var counterpick2 = Convert.ToInt32(counterpicks[1]);
            var counterpick3 = Convert.ToInt32(counterpicks[2]);
            return
                $"Counter-picks for chosen hero:  {Heroes.GetName(counterpick1)}, {Heroes.GetName(counterpick2)}, {Heroes.GetName(counterpick3)}";
    }

    
    [HttpGet("/heroes/{heroID:int}/matchup")]
    public async Task<string> GetMatchups(string heroID)
    {
        var request = await new HttpClient().GetAsync($"https://api.opendota.com/api/heroes/{heroID}/matchups");
        var response = await request.Content.ReadAsStringAsync();
        var unsortedBody = JsonObject.Parse(response);
        double firstWinrate = 1;
        int firstBan=0;
        double secondWinrate = 1;
        int secondBan=0;
        double thirdWinrate = 1;
        int thirdBan=0;
        foreach (var hero in unsortedBody.AsArray())
        {
            var hero_id = (int)hero["hero_id"];
            double winrate = (double)hero["wins"] / (double)hero["games_played"];
            if (winrate<firstWinrate && hero_id!=firstBan)
            {
                thirdBan = secondBan;
                thirdWinrate = secondWinrate;
                
                secondBan = firstBan;
                secondWinrate = firstWinrate;
                
                firstBan = hero_id;
                firstWinrate = winrate;
            }

            if (winrate < secondWinrate && hero_id!=firstBan && hero_id!=secondBan)
            {
                thirdBan = secondBan;
                thirdWinrate = secondWinrate;
                
                secondBan = hero_id;
                secondWinrate = winrate;
            }

            if (winrate < thirdWinrate && hero_id!=secondBan && hero_id!=firstBan && hero_id!=thirdBan)
            {
                thirdBan = hero_id;
                thirdWinrate = winrate;
            }
        }

        return $"{firstBan}+{secondBan}+{thirdBan}";
    }

    [HttpGet("/hero_promatches/{heroID:int}")]
    public async Task<string> GetProMatches(int heroID)
    {
        var request = await new HttpClient().GetAsync($"https://api.opendota.com/api/heroes/{heroID}/matches");
        var response = await request.Content.ReadAsStringAsync();
        var match = JsonObject.Parse(response);
        long matchID = 0;
        string leagueName="";
        foreach (var id in match.AsArray())
        {
            if (matchID == 0)
            {
                matchID = Convert.ToInt64(id["match_id"].ToString());
                leagueName = id["league_name"].ToString();
            }
        }
        var matchRequest = await new HttpClient().GetAsync($"https://api.opendota.com/api/matches/{matchID}");
        var matchResponse = await matchRequest.Content.ReadAsStringAsync();
        var matchUrl = (string)JsonObject.Parse(matchResponse)["replay_url"];
        return $"Name of PRO League:  {leagueName}\n\nReply download url:  {matchUrl}";
    }

    private async Task<string> Get32Id(long id)
    {
        // MySqlCommand selectID = new MySqlCommand("SELECT telegram_id FROM account_id.id WHERE id = 311739400", database.getConnection());
        // database.OpenConnection();
        // var _test = selectID.ExecuteReader();
        // string telegram="";
        // if (_test.Read())
        // {
        //     telegram = _test.GetString(_test.GetOrdinal("telegram_id"));
        // }
        var request = await new HttpClient().GetAsync($"https://api.opendota.com/api/players/{id}");
        var answer = await request.Content.ReadAsStringAsync();
        var profileurl = (string)JsonObject.Parse(answer)["profile"]["profileurl"];
        var dotaPlus = (bool)JsonObject.Parse(answer)["profile"]["plus"];
        var mmr = (int)JsonObject.Parse(answer)["mmr_estimate"]["estimate"];
        var nickname = (string)JsonObject.Parse(answer)["profile"]["personaname"];
        string response = $"\tYou've successfully set Steam profile to use with bot!\n---------------------------------------------------------\n  Your nickname:\t    {nickname}\n  MMR/Rank:     ";
        if (dotaPlus)
        {
            response += $"<{mmr}>       DotaPlus:   ✅\n\n";
        }

        if (!dotaPlus)
        {
            response += $"<{mmr}>       DotaPlus:   ❌\n\n";
        }

        return response + $"Your steam account: {profileurl}"; // + telegram;
    }

    private async Task<string> GetMatchID(long matchID, long id)
    {
        var rp = await new HttpClient().PostAsync($"https://api.opendota.com/api/request/{matchID}", new StringContent("", Encoding.Unicode, "application/json"));
        Thread.Sleep(10000);
        var request = await new HttpClient().GetAsync($"https://api.opendota.com/api/matches/{matchID}");
        var response = "";
        var answer = await request.Content.ReadAsStringAsync();
        var unsortedBody = JsonObject.Parse(answer);
        foreach (var player in unsortedBody["players"].AsArray())
        {
            var test = (int)player["obs_placed"];
            if (player["account_id"] != null)
            {
                if (Convert.ToInt32(player["account_id"].ToString()) == id)
                {
                    if((int)player["obs_placed"] > 3 && (int)player["sen_placed"] > 4)
                    {
                        var analyzeSup = new AnalyzingParametrs((int)player["duration"]);

                        string sentry_analyze = null;
                        if (analyzeSup.AvgSentryPerMin() > (int)player["sen_placed"])
                        {
                            sentry_analyze = "You placed less sentries, than avarage for this time. Try to bought more sentries to up your de-warding skills";
                        }
                        if (analyzeSup.AvgSentryPerMin() < (int)player["sen_placed"])
                        {
                            sentry_analyze = "You placed more sentries, than avarage for this time. Good de-warding!";
                        }

                        string obs_analyze = null;
                        if (analyzeSup.AvgObsPerMin() > (int)player["obs_placed"])
                        {
                            obs_analyze = "You placed less obs, than avarage for this time. Try to bought more obs to give more vision for your team";
                        }
                        if (analyzeSup.AvgObsPerMin() < (int)player["obs_placed"])
                        {
                            obs_analyze = "You placed more obs, than avarage for this time. Good warding!";
                        }

                        string lane_analyze = null;
                        switch ((double)player["lane_efficiency"])
                        {
                            case >0.75:
                                lane_analyze = "You acted very good on the line. Nice job!";
                                break;
                            case <0.75 and >0.5:
                                lane_analyze = "You acted good on the line. Try better! Do not forget to do withdrawals and block enemies big camp";
                                break;
                            case <0.5:
                                lane_analyze = "You acted bad on the line. Watch this match again and analyze your actions from start of game to 7:00. Try to play more safety, buy more heal on line and pick strong-line heroes.";
                                break;
                        }

                        string teamfights_analyze = null;
                        switch ((double)player["teamfight_participation"])
                        {
                            case >0.5:
                                teamfights_analyze = "You often connect teamfights, that good mark, because support's role in fight are very necessary.";
                                break;
                            case <0.5:
                                teamfights_analyze = "You need to connect teamfights more, that is obligatorily for supports role! And do not forget to use your skills in fight.";
                                break;
                        }
                        response =$"Ward analyze:\n{sentry_analyze}\n{obs_analyze}\n\nLane efficiency:\n{lane_analyze}\n\nTeamfights:\n{teamfights_analyze}";
                        break;
                    }

                    if ((double)player["obs_placed"] <= 3 && (double)player["sentry_uses"] <= 4)
                    {
                        var analyzeCore = new AnalyzingParametrs();
                        string teamfights_analyze = null;
                        switch ((double)player["teamfight_participation"])
                        {
                            case >0.5:
                                teamfights_analyze = "You often connect teamfights, that good index for core role!";
                                break;
                            case <0.5:
                                teamfights_analyze = "You need to connect teamfights more, your teammates need you! Try to mix farm patterns with movements of your team. Also call fights after getting important items.";
                                break;
                        }
                        string lane_analyze = null;
                        switch ((double)player["lane_efficiency"])
                        {
                            case >0.75:
                                lane_analyze = "You acted very good on the line. Nice job!";
                                break;
                            case <0.75 and >0.5:
                                lane_analyze = "You acted good on the line. Try better!";
                                break;
                            case <0.5:
                                lane_analyze = "You acted bad on the line. Watch this match again and analyze your actions from start of game to 7:00";
                                break;
                        }
                        
                        string gold_analyze = null;
                        switch ((double)player["benchmarks"]["gold_per_min"]["pct"])
                        {
                            case >0.75:
                                gold_analyze = "Your farm style and efficiency are good!";
                                break;
                            case <0.75 and >0.5:
                                gold_analyze = "Your farm is ok, but you need to try get more gold during the game.";
                                break;
                            case <0.5:
                                gold_analyze = "Your farm skills are lower than avarage, watch replays of games and some guide videos about farm patterns.";
                                break;
                        }

                        response = $"Teamfights:\n{teamfights_analyze}\n\nLane efficiency:\n{lane_analyze}\n\nFarm skills:\n{gold_analyze}";
                    }
                    return response;
                }
            }
            else
            {
                response = "You haven't played this match";
            }
        }
        return response;
    }
}