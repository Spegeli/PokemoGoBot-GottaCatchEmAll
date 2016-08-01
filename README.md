<!-- define warning icon -->
[1.1]: http://i.imgur.com/M4fJ65n.png (ATTENTION)
[1.2]: http://i.imgur.com/NNcGs1n.png (BTC)
<!-- title -->
<h1>A Pokemon Go Bot based on FeroxRevs API</h1>
<!-- disclaimer -->
![alt text][1.1] <strong><em> The contents of this repo are a proof of concept and are for educational use only </em></strong>![alt text][1.1]
![](https://github.com/Spegeli/Pokemon-Go-Rocket-API/blob/master/Screenshot.png)
Chatting about this Repository can be done on our Discord: https://discord.gg/nJNh2PM <br/>
<br/>

<h2><a name="features">Features</a></h2>
 
 - [PTC Login / Google]
 - [Humanlike Walking]<br />
 - [Configurable Custom Pathing]<br />
   (Speed in km/h is configurable via UserSettings)
 - [Farm Pokestops]<br />
   (use always the nearest from the current location)<br />
   (Optional: keep within specific MaxTravelDistanceInMeters to Start Point) (MaxTravelDistanceInMeters configurable via UserSettings)
 - [Farm all Pokemon near your]<br />
   (Optional: PokemonsNotToCatch List. Disabled by default, can be Enabled via UserSettings, configurable Names via File in Config Folder)
 - [Evolve Pokemon]<br />
   (Optional: Enabled by default, can be Disabled via UserSettings)<br />
   (Optional: PokemonsToEvolve List - Only Pokemons in this List will be Evolved, configurable via File in Config Folder)<br />
   (Optional: EvolveOnlyPokemonAboveIV - Will Evolve only Pokemon with IV > EvolveAboveIVValue, Disabled by default, can be Enabled vis UserSettings)
 - [Transfer Pokemon]<br />
   (ignore favorite/gym marked)<br />
   (Optional: Enabled by default, can be Disabled via UserSettings.)<br />
   (Optional: UseTransferPokemonKeepAboveCP - Keeps all Pokemon with CP > TransferPokemonKeepAboveCP, Enabled by default, can be Disabled vis UserSettings)
   (Optional: UseTransferPokemonKeepAboveIV - Keeps all Pokemon with IV > TransferPokemonKeepAboveIVPercentage, Enabled by default, can be Disabled vis UserSettings)
   (Optional: PrioritizeIVOverCP - Determines the sorting sequence - CP or IV, Enabled by default, can be Disabled via UserSettings.)<br />
   (Optional: TransferPokemonKeepDuplicateAmount - The amount of X best Pokemon he should keep, 2 by default, configurable via UserSettings)<br />
   (Optional: PokemonsNotToTransfer List. Enabled by default, can be Disabled via UserSettings, configurable Names via File in Config Folder)
   (Optional: NotTransferPokemonsThatCanEvolve - Will keep ALL Pokemons which can be Evolve not matter if they on PokemonsToEvolve List or not, Disabled by default, can be Enabled via UserSettings)
 - [Throws away unneeded items]<br />
   (configurable via Settings.cs)
 - [Use Lucky Eggs]<br />
   (Disbaled by default, can be Enabled via UserSettings)
 - [Use Incense]<br />
   (Disbaled by default, can be Enabled via UserSettings)
 - [Use best Pokeball & Berry]<br />
   (depending on Pokemon CP and IV)
 - [Creates Excel CSV File on Startup with your current Pokemon]<br />
   (including Number, Name, CP,IV Perfection in % and many more) (can be found in the Export Folder)
 - [Softban bypass]
 - [Log File System]<br />
   (all activity will be tracked in a Log File)
 - [Statistic in the Header] ![alt tag](https://github.com/Spegeli/Pokemon-Go-Rocket-API/blob/master/StatisticScreenshot.png)
 - [Very color and useful Logging]<br />
   (so you every time up2date what currently happened)
 - and many more ;-)
<br/>

<h2><a name="screenshots">Screenshots</a></h2><br/>
- coming soon -<br/>
<hr/>
<br/>

<h2><a name="getting-started">Getting Started</a></h2>
Make sure you check out our [Wiki](https://github.com/Spegeli/PokemoGoBot-GottaCatchEmAll/wiki) to get started.
<br/>

<h2><a name="credits">Credits</a></h2>
A big thank you goes to Feroxs' hard work on the API & Console. Without him, this would not have been possible. <3
<br/>
Thanks to everyone who volunteered by contributing via Pull Requests!

<h2><a name="donating">Donating</a></h2>
<a name="paypal">Feel free to buy us all a beer, by using PayPal:</a><br/>
[![](http://i.epvpimg.com/vI2ch.gif)](https://www.paypal.me/MoneyForSpegeli) OR 
[![](http://i.epvpimg.com/vI2ch.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=YE3KBU2RGZYPY) <br/>

<h6><em>[ All PayPal donations are distributed amongst our most active collaborators. ]</em></h6><br/>
<a name="btc">Donate Bitcoins to FeroxRev (the API library developer): *1ExYxfBb5cERHyAfqtFscJW7vm2vWBbL3e*</a><br/>

<hr/>