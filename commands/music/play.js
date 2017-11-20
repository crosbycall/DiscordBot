const commando = require('discord.js-commando');

var now_playing = null;
var queue = [];
var voice_connection = null;
var voice_handler = null;
var text_channel = null;
var yt_api = null;

class MusicPlayCommand extends commando.Command{
    constructor(client){
        super(client, {
            name: 'play',
            group: 'music',
            memberName: 'play',
            description: 'Plays local music'
        });
    }

    async run(message, args){

        message.say("Adding to queue: " + args);
    }
}

module.exports=MusicPlayCommand;