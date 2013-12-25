fs = require 'fs'
module.exports = (grunt) ->
	grunt.initConfig
		pkg: grunt.file.readJSON('package.json')
		sass:
			css:
				src:  'css/rohpod.scss'
				dest: 'build/css/rohpod.css'
		copy:
			js:
				src:  'js/*.js'
				dest: 'build/'
				expand:  true
				flatten: true
			img:
				src:  'img/*'
				dest: 'dist/'
				expand:  true
				flatten: true
			# jslib:
			# 	src:  'build/jslibs.min.js'
			# 	dest: 'dist/jslibs.min.js'
			index:
				src:  'index.htm'
				dest: 'build/'
			deploy:
				src:  'build/*'
				dest: 'dist/'
				expand: true
				flatten: true
				filter: 'isFile'
		concat:
			jslib:
				src:  'jslib/*.min.js'
				dest: 'build/jslibs.min.js'
		clean:
			dist:  'dist'
			build: 'build'

		myth:
			css:
				src:  'build/css/rohpod.css'
				dest: 'build/style.css'

	grunt.loadNpmTasks 'grunt-contrib-sass'
	grunt.loadNpmTasks 'grunt-contrib-copy'
	grunt.loadNpmTasks 'grunt-contrib-concat'
	grunt.loadNpmTasks 'grunt-contrib-clean'
	grunt.loadNpmTasks 'grunt-myth'

	grunt.registerTask 'default', [
		'setup'
		'clean:dist'
		'css'
		'js'
		'templates'
		'misc'
		'copy:deploy'
	]

	grunt.registerTask 'setup', () ->
		fs.mkdirSync 'build' unless fs.existsSync 'build'
		grunt.task.run 'concat:jslib' unless fs.existsSync 'build/jslibs.min.js'

	grunt.registerTask 'css', [
		'sass:css'
		'myth:css'
	]

	grunt.registerTask 'js', [
		'copy:js'
	]

	grunt.registerTask 'templates', [
		'copy:index'
	]

	grunt.registerTask 'misc', [
		'copy:img'
	]

