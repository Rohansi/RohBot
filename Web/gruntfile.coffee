fs = require 'fs'
module.exports = (grunt) ->
	grunt.initConfig
		pkg: grunt.file.readJSON('package.json')
		# CSS
		less:
			options:
				strictMath: true
				strictUnits: true
				sourceMap: false # We're going to be post-processing the compiled output
			css:
				src:  'css/rohbot.less'
				dest: 'build/css/rohbot.css'
		myth:
			css:
				src:  'build/css/rohbot.css'
				dest: 'build/style.css'
		# JS
		coffee:
			classes:
				src: 'js/classes/*.coffee'
				dest: 'build/js/rohbot-classes.js'
			js:
				src:  'js/*.coffee'
				dest: 'build/js/rohbot-coffee.js'
		concat:
			js:
				src:  ['build/js/rohbot-classes.js','build/js/rohbot-coffee.js', 'build/js/rohbot.js']
				dest: 'build/rohbot.js'
			jslib:
				src:  'jslib/*.min.js'
				dest: 'build/jslibs.min.js'
		# Other
		copy:
			js:
				src:  'js/*.js'
				dest: 'build/js/'
				expand:  true
				flatten: true
			img:
				src:  'img/*'
				dest: 'dist/'
				expand:  true
				flatten: true
			index:
				src:  'index.htm'
				dest: 'build/'
			deploy:
				src:  'build/*'
				dest: 'dist/'
				expand: true
				flatten: true
				filter: 'isFile'
		clean:
			dist:  'dist'
			build: 'build'

	grunt.loadNpmTasks 'grunt-contrib-less'
	grunt.loadNpmTasks 'grunt-contrib-copy'
	grunt.loadNpmTasks 'grunt-contrib-concat'
	grunt.loadNpmTasks 'grunt-contrib-clean'
	grunt.loadNpmTasks 'grunt-contrib-coffee'
	grunt.loadNpmTasks 'grunt-myth'

	grunt.registerTask 'default', [
		'setup'
		'clean:dist'
		'css'
		'js'
		'templates'
		'misc'
		'deploy'
	]

	grunt.registerTask 'setup', () ->
		fs.mkdirSync 'build' unless fs.existsSync 'build'
		grunt.task.run 'concat:jslib' unless fs.existsSync 'build/jslibs.min.js'

	grunt.registerTask 'css', [
		'less:css'
		'myth:css'
	]

	grunt.registerTask 'js', [
		'coffee:classes'
		'coffee:js'
		'copy:js'
		'concat:js'
	]

	grunt.registerTask 'templates', [
		'copy:index'
	]

	grunt.registerTask 'misc', [
		'copy:img'
	]

	grunt.registerTask 'deploy', [
		'copy:deploy'
	]

