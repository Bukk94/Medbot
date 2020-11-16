# Medbot

[![GitHub version](https://img.shields.io/badge/version-0.4.0-brightgreen.svg)](https://github.com/Bukk94/Medbot/releases)
[![Build Status](https://travis-ci.com/Bukk94/Medbot.svg?token=XTeWt6KEyExzbH1iNFWD&branch=master)](https://travis-ci.com/Bukk94/Medbot)
[![Dependency Status](https://david-dm.org/boennemann/badges.svg)](/packages.config)

IRC loyalty Twitch bot in development for YouTube channel [Medvědí Doupě](https://www.youtube.com/Bukk94).

Medbot is actively scanning selected Twitch channel, reading chat messages, 
rewards active viewers by giving them virtual points and experience points, and 
reacts to commands.

Medbot is mainly developed for Windows ([Medbot_UI](/Medbot_UI) project) but can be run also on Linux, 
using some C# emulators like mono ([Medbot_CLI](/Medbot_CLI) project). 
The bot itself can be found in the [Medbot](/Medbot) project.

## Loyalty points

Bot rewards all users on the stream. Bot detects new users as well as parting users.
Depending on the settings and user activity, the bot rewards viewers by giving them virtual currency and
experience points. Usually, active users gain more points.

Based on experience points can viewers level-up and gain ranks. Each rank can be customized in the `Rank.txt` file.

Viewers can spend their virtual currency on various commands or goods, also fully customizable in the `Commands.xml` file.

## Bot commands

The bot supports a wide range of commands, featuring:
- Loyalty point system
- Experience ranking
- Gambling
- Many misc commands (colors, random selection, etc.)

All commands can be customized in the `Commands.xml` file.

## Settings

All settings are stored in the `Settings.xml` file. This file contains OAuth tokens, 
ranking and point interval limits, rewards, blacklist, and many more.

## Dictionary

The bot can be used in many different languages. Just modify the `Dictionary.json` file and translate all the strings!

## Getting an OAuth token

The bot needs an IRC Twitch OAuth token to work. Login with your Twitch account and generate an OAuth token
using following page [http://twitchapps.com/tmi/](http://twitchapps.com/tmi/).
Note that this is a safe way to generate tokens, and you can always reject access.

## Using the Medbot

Do you want to use this bot? No problem! Just let me know **Bukk94[at]seznam.cz**.