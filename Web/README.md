RohBot
==========
RohBot is the front-end app for the SteamMobile service. It's written in [TypeScript][ts], uses [LESS][less] + [Myth][myth] for CSS preprocessing, [Mustache][mustache] for templates and [Grunt][grunt] to bring everything together.

Compiling
---------
First, make sure you have [Node.js][node] installed. Then run
```
npm install -g grunt-cli
npm install
```
from this directory to set up the necessary dependencies.  
The project can then be built by simply running `grunt`.

There is a staging folder called `build` which is useful for partial builds. First make sure you've done a full build, then you can incrementally build by specifying which part to build, such as
```
grunt css
```
or
```
grunt js
```
That will update the staging folder, the contents of which can then be pushed into dist/ with
```
grunt dist
```
Helpfully, grunt lets you string commands together, so you could run
```
grunt js templates dist
```
to update both the javascript and template files and push the result.

TODO
----

- Cleanup
	- Convert monolithic rohbot.js into managable chunks
	- Use templates for managing injected HTML
	- Update HTML to conform to HTML semantics and accessability rules
	- Make everything far more complex than it was before
- Features
	- Options menu to replace various / commands
		- Store password
		- Enable notifications w/ one or more of:
			- On Name
			- On Regex
			- Every message
		- Change timestamps
			- 12Hr / 24Hr
			- Remove
	- Permanent name list on side like steam chat
	- Tabs for multiple rooms + PMs

[ts]: http://www.typescriptlang.org/
[less]: http://lesscss.org/
[myth]: http://www.myth.io/
[mustache]: http://mustache.github.io/mustache.5.html
[grunt]: http://gruntjs.com
[node]: http://nodejs.org/
