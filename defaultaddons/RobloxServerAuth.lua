local this = {}

-- RobloxServer authentication (2015 style authentication tickets).
-- Only runs on servers started from the RobloxServer game browser: the launcher sets
-- _G.RSBaseUrl, _G.RSJobId, _G.RSServerKey and _G.RSRequireAuth before CSServer runs.
-- Every player must hand over the one-time ticket the website gave them (the launcher adds an
-- "RSAuthTicket" value to the player). The ticket is checked with /Game/ValidateTicket.ashx and
-- players without a valid ticket, with another name or with a banned account are kicked.

-- set this to false to only log failed checks instead of kicking (the website setting still applies)
local kickOnFailure = true

-- DONT EDIT ANYTHING ELSE BELOW

function this:Name()
	return "RobloxServer Authentication"
end

function this:IsEnabled(Script, Client)
	return (Script == "Server" and _G.RSJobId ~= nil and _G.RSJobId ~= "")
end

local function HttpGet(url)
	local ok, result = pcall(function() return game:HttpGet(url, true) end)
	if (not ok or result == nil) then
		ok, result = pcall(function() return game:HttpGet(url) end)
	end
	if (ok) then
		return result
	end
	return nil
end

-- Some old clients only allow http://www.roblox.com, which the Raspberry Pi DNS or the
-- RobloxServerBridge web proxy extension sends to the RobloxServer.
local function Request(path)
	local result = HttpGet(_G.RSBaseUrl .. path)
	if (result == nil or result == "") then
		result = HttpGet("http://www.roblox.com/" .. path)
	end
	return result
end

local function Split(text)
	local fields = {}
	local start = 1
	while true do
		local i = string.find(text, "|", start, true)
		if (i == nil) then
			table.insert(fields, string.sub(text, start))
			break
		end
		table.insert(fields, string.sub(text, start, i - 1))
		start = i + 1
	end
	return fields
end

local function FindChild(parent, name)
	local found = nil
	pcall(function() found = parent:FindFirstChild(name) end)
	if (found == nil) then
		pcall(function() found = parent:findFirstChild(name) end)
	end
	return found
end

local function Kick(Player, reason)
	print("RobloxServer: '" .. Player.Name .. "' failed authentication: " .. reason)
	if (not kickOnFailure) then
		return
	end

	local kicked = pcall(function() KickPlayer(Player, reason) end)
	if (kicked) then
		return
	end

	-- same fallback as ServerWhitelist.lua
	local Server = game:GetService("NetworkServer")
	for _,Child in pairs(Server:children()) do
		local ok, owner = pcall(function() return Child:GetPlayer() end)
		if (ok and owner == Player) then
			delay(0.3, function() Child:CloseConnection() end)
		end
	end
end

local function Authenticate(Player)
	local value = nil
	for i = 1, 100 do
		value = FindChild(Player, "RSAuthTicket")
		if (value ~= nil and value.Value ~= "") then
			break
		end
		wait(0.1)
	end

	if (Player.Parent == nil) then
		return
	end

	if (value == nil or value.Value == "") then
		if (_G.RSRequireAuth) then
			Kick(Player, "No authentication ticket. Join this game through the RobloxServer game browser.")
		end
		return
	end

	local ticket = value.Value
	-- other clients can see the player's children, do not leave the ticket around
	pcall(function() value.Value = "" end)
	pcall(function() value.Parent = nil end)

	local answer = Request("Game/ValidateTicket.ashx?ticket=" .. ticket .. "&jobId=" .. _G.RSJobId .. "&serverKey=" .. _G.RSServerKey)
	if (answer == nil or answer == "") then
		if (_G.RSRequireAuth) then
			Kick(Player, "Could not reach the RobloxServer to verify your account.")
		end
		return
	end

	-- OK|userId|userName|superSafeChat|accountAge|isAdmin  or  ERROR|reason
	local fields = Split(answer)
	if (fields[1] ~= "OK") then
		Kick(Player, fields[2] or "Invalid authentication ticket.")
		return
	end

	if (Player.Name ~= fields[3]) then
		Kick(Player, "Your name does not match your RobloxServer account (" .. fields[3] .. ").")
		return
	end

	if (fields[4] == "true") then
		pcall(function() Player:SetSuperSafeChat(true) end)
	end

	local userId = Instance.new("StringValue")
	userId.Name = "RSUserId"
	userId.Value = fields[2]
	userId.Parent = Player
	print("RobloxServer: '" .. Player.Name .. "' authenticated as user " .. fields[2] .. ".")
end

function this:PostInit()
	if (_G.RSFilteringEnabled) then
		pcall(function() game.Workspace.FilteringEnabled = true end)
	end
	print("RobloxServer: authentication enabled for job " .. _G.RSJobId .. (_G.RSRequireAuth and " (tickets required)" or ""))
end

function this:OnPlayerAdded(Player)
	-- wait() cannot be used inside pcall in these clients, so run in a plain coroutine
	local ok, err = coroutine.resume(coroutine.create(function() Authenticate(Player) end))
	if (not ok) then
		print("RobloxServer: authentication error: " .. tostring(err))
	end
end

function this:OnPlayerRemoved(Player)
	if (FindChild(Player, "RSUserId") ~= nil) then
		coroutine.resume(coroutine.create(function()
			Request("Game/Servers.ashx?action=playerleft&jobId=" .. _G.RSJobId .. "&serverKey=" .. _G.RSServerKey)
		end))
	end
end

-- DO NOT REMOVE THIS. this is required to load this addon into the game.

function AddModule(t)
	print("AddonLoader: Adding " .. this:Name())
	table.insert(t, this)
end

_G.CSScript_AddModule=AddModule
