const commando = require('discord.js-commando');
const bot = new commando.Client();
//const Discord = require('discord.js');
//const bot = new Discord.Client();

bot.registry.registerGroup('random', 'Random');
bot.registry.registerGroup('music', 'Music');
bot.registry.registerGroup('files', 'Files');
bot.registry.registerDefaults();
bot.registry.registerCommandsIn(__dirname + "/commands");

bot.login('MjkxMDg0ODY2NjcyNjU2Mzk1.C6kVbw.POaNLxNE-mMkREqCzpnEPiC41hA');

