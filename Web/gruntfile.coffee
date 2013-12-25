fs = require 'fs'
module.exports = (grunt) ->
	grunt.initConfig
		pkg: grunt.file.readJSON('package.json')
		sass: main:
			files:
				'dist/style.css': 'css/rohpod.scss'
		copy: main:
			files: [
				{ dest: 'dist/', src: ['index.htm'] }
				{ expand: true, flatten: true, dest: 'dist/', src: ['js/*.js'] }
				{ expand: true, flatten: true, dest: 'dist/', src: ['img/*'] }
			]
		concat:
			libs:
				src: ['jslib/*.min.js']
				dest: 'dist/jslibs.min.js'
	grunt.loadNpmTasks 'grunt-contrib-sass'
	grunt.loadNpmTasks 'grunt-contrib-copy'
	grunt.loadNpmTasks 'grunt-contrib-concat'
	grunt.registerTask 'default', ['copy', 'sass']
	grunt.registerTask 'clean', () ->
		rmdir = (path) ->
			return unless fs.existsSync path
			fs.readdirSync( path ).forEach (file) ->
				fs.unlinkSync( path + '/' + file )
			fs.rmdirSync path

		rmdir 'dist'
		rmdir 'build'
