fx_version 'cerulean'
resource_type 'gametype' { name = 'Survive the Hunt' }

client_scripts { 
	'SurviveTheHuntClient.net.dll', 
	'SurviveTheHuntClient.Models.net.dll', 
	'SurviveTheHuntClient.Plugins.Xmas.net.dll',
	'SurviveTheHuntClient.Plugins.Cupid.net.dll'
}
server_scripts { 'SurviveTheHuntServer.net.dll' }
shared_scripts { 'SurviveTheHuntShared.net.dll' }

game 'gta5'