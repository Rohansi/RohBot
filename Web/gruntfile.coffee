fs = require 'fs'
module.exports = (grunt) ->
	grunt.initConfig
		pkg: grunt.file.readJSON('package.json')
		sass: main:
			files:
				'dist/style.css': 'css/rohpod.scss'
		copy:
			js:
				src:  'js/*.js'
				dest: 'dist/'
				expand:  true
				flatten: true
			img:
				src:  'img/*'
				dest: 'dist/'
				expand:  true
				flatten: true
			jslib:
				src:  'build/jslibs.min.js'
				dest: 'dist/jslibs.min.js'
			index:
				src:  'index.htm'
				dest: 'dist/'
		concat:
			jslib:
				src:  'jslib/*.min.js'
				dest: 'build/jslibs.min.js'
		clean:
			dist:  'dist'
			build: 'build'

		myth:
			css:
				src:  'build/rohpod.css'
				dest: 'dist/style.css'


	grunt.loadNpmTasks 'grunt-contrib-sass'
	grunt.loadNpmTasks 'grunt-contrib-copy'
	grunt.loadNpmTasks 'grunt-contrib-concat'
	grunt.loadNpmTasks 'grunt-contrib-clean'
	grunt.loadNpmTasks 'grunt-myth'
	grunt.registerTask 'default', () ->
		fs.mkdirSync 'build' unless fs.existsSync 'build'
		grunt.task.run 'concat:jslib' unless fs.existsSync 'build/jslibs.min.js'
		grunt.task.run [
			'clean:dist'
			'copy'
			# 'sass'
		]
