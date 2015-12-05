var fs = require('fs');

module.exports = function(grunt) {
    grunt.initConfig({
        pkg: grunt.file.readJSON('package.json'),
		
        // CSS
        less: {
            options: {
                strictMath: true,
                strictUnits: true,
                sourceMap: false // We're going to be post-processing the compiled output
            },
            css: {
                src: 'css/rohbot.less',
                dest: 'build/style.css'
            }
        },
        myth: {
            css: {
                src: 'build/style.css',
                dest: 'build/style.css'
            }
        },
        cssmin: {
            css: {
                src: 'build/style.css',
                dest: 'dist/style.css'
            }
        },

        // JavaScript
        typescript: {
            js: {
                src: ['js/RohStore.ts', 'js/**/*.ts'],
                dest: 'build/rohbot.js'
            }
        },
        concat: {
            js: {
                src: ['build/rohbot.js', 'js/init.js'],
                dest: 'build/rohbot.js'
            },
            jslib: {
                src: 'jslib/*.min.js',
                dest: 'build/jslibs.min.js'
            }
        },
		uglify: {
		    js: {
		        src: 'build/rohbot.js',
		        dest: 'dist/rohbot.js'
		    }
		},

        // HTML (including Templates)
        html_minify: {
            options: {
                removeComments: true,
                collapseWhitespace: true
            },
            index: {
                src: 'build/index.htm',
                dest: 'dist/index.htm'
            },
            templates: {
                src: 'templates/*',
                dest: 'build/',
                expand: true
            },
        },
        hogan: {
            templates: {
                templates: 'build/templates/*.mustache',
                output: 'build/templates.js',
                binderName: 'hulk'
            }
        },
		
        // Other
        copy: {
            index: {
                src: ['index.htm', 'manifest.json'],
                dest: 'build/'
            },
            img: {
                src: 'img/*',
                dest: 'build/',
                expand: true,
                flatten: true,
                filter: 'isFile'
            },
            dist: {
                src: ['manifest.json', 'img/*', 'build/jslibs.min.js', 'build/templates.js'],
                dest: 'dist/',
                expand: true,
                flatten: true,
                filter: 'isFile'
            }
        },
        
        clean: {
            dist: 'dist',
            build: 'build'
        }
    });

    grunt.loadNpmTasks('grunt-contrib-less');
    grunt.loadNpmTasks('grunt-contrib-copy');
    grunt.loadNpmTasks('grunt-contrib-concat');
    grunt.loadNpmTasks('grunt-contrib-clean');
    grunt.loadNpmTasks('grunt-html-minify');
    grunt.loadNpmTasks('grunt-contrib-uglify');
    grunt.loadNpmTasks('grunt-contrib-cssmin');
    grunt.loadNpmTasks('grunt-myth');
    grunt.loadNpmTasks('grunt-hogan');
    grunt.loadNpmTasks('grunt-typescript');

    grunt.registerTask('default', [
        'setup',
        'clean:dist',
        'css',
        'js',
        'html',
        'img',
        'dist'
    ]);

    grunt.registerTask('setup', function() {
        if (!fs.existsSync('build'))
            fs.mkdirSync('build');

        if (!fs.existsSync('build/jslibs.min.js'))
            grunt.task.run('concat:jslib');
    });

    grunt.registerTask('css', [
        'less:css',
        'myth:css'
    ]);

    grunt.registerTask('js', [
        'typescript:js',
        'concat:js'
    ]);

    grunt.registerTask('html', [
        'copy:index',
        'html_minify:templates',
        'hogan:templates'
    ]);

    grunt.registerTask('img', [
        'copy:img'
    ]);

    grunt.registerTask('dist', [
        'copy:dist',
        'html_minify:index',
        'uglify:js',
        'cssmin:css'
    ]);
};
