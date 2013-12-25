module.exports = (grunt) ->
	grunt.initConfig
		pkg: grunt.file.readJSON('package.json')
		sass: main:
			files:
				'dist/style.css': 'css/rohpod.scss'
		copy: main:
			files: [
				{ expand: true, flatten: true, dest: 'dist/', src: ['jslib/*.min.js'], filter: 'isFile' }
				{ expand: true,                dest: 'dist/', src: ['index.htm'] }
				{ expand: true, flatten: true, dest: 'dist/', src: ['js/*.js'] }
				{ expand: true, flatten: true, dest: 'dist/', src: ['img/*'] }
			]
	grunt.loadNpmTasks 'grunt-contrib-sass'
	grunt.loadNpmTasks 'grunt-contrib-copy'
	grunt.registerTask 'default', ['copy', 'sass']
