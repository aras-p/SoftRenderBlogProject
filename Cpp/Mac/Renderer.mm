#import <simd/simd.h>
#import <ModelIO/ModelIO.h>
#include <mach/mach_time.h>

const int kBackbufferWidth = 1920;
const int kBackbufferHeight = 1080;

#import "Renderer.h"
#include "../Source/Softy.h"
#define STB_IMAGE_IMPLEMENTATION
#include "../Source/stb_image.h"

static const NSUInteger kMaxBuffersInFlight = 3;


@implementation Renderer
{
    dispatch_semaphore_t _inFlightSemaphore;
    id <MTLDevice> _device;
    id <MTLCommandQueue> _commandQueue;

    id <MTLRenderPipelineState> _pipelineState;
    id <MTLDepthStencilState> _depthState;
    MTLVertexDescriptor *_mtlVertexDescriptor;
    
    id <MTLTexture> _backbuffer;
    uint32_t* _backbufferPixels;

    MTKMesh *_mesh;
    mach_timebase_info_data_t _clock_timebase;
}

-(nonnull instancetype)initWithMetalKitView:(nonnull MTKView *)view;
{
    self = [super init];
    if(self)
    {
        _device = view.device;
        _inFlightSemaphore = dispatch_semaphore_create(kMaxBuffersInFlight);
        mach_timebase_info(&_clock_timebase);
        [self _loadMetalWithView:view];
        [self _loadAssets];
    }

    return self;
}

- (void)_loadMetalWithView:(nonnull MTKView *)view;
{
    view.depthStencilPixelFormat = MTLPixelFormatDepth32Float_Stencil8;
    view.colorPixelFormat = MTLPixelFormatBGRA8Unorm;
    view.sampleCount = 1;

    _mtlVertexDescriptor = [[MTLVertexDescriptor alloc] init];

    _mtlVertexDescriptor.attributes[0].format = MTLVertexFormatFloat3;
    _mtlVertexDescriptor.attributes[0].offset = 0;
    _mtlVertexDescriptor.attributes[0].bufferIndex = 0;

    _mtlVertexDescriptor.attributes[1].format = MTLVertexFormatFloat2;
    _mtlVertexDescriptor.attributes[1].offset = 0;
    _mtlVertexDescriptor.attributes[1].bufferIndex = 1;

    _mtlVertexDescriptor.layouts[0].stride = 12;
    _mtlVertexDescriptor.layouts[0].stepRate = 1;
    _mtlVertexDescriptor.layouts[0].stepFunction = MTLVertexStepFunctionPerVertex;

    _mtlVertexDescriptor.layouts[1].stride = 8;
    _mtlVertexDescriptor.layouts[1].stepRate = 1;
    _mtlVertexDescriptor.layouts[1].stepFunction = MTLVertexStepFunctionPerVertex;

    id<MTLLibrary> defaultLibrary = [_device newDefaultLibrary];

    id <MTLFunction> vertexFunction = [defaultLibrary newFunctionWithName:@"vertexShader"];

    id <MTLFunction> fragmentFunction = [defaultLibrary newFunctionWithName:@"fragmentShader"];

    MTLRenderPipelineDescriptor *pipelineStateDescriptor = [[MTLRenderPipelineDescriptor alloc] init];
    pipelineStateDescriptor.label = @"MyPipeline";
    pipelineStateDescriptor.sampleCount = view.sampleCount;
    pipelineStateDescriptor.vertexFunction = vertexFunction;
    pipelineStateDescriptor.fragmentFunction = fragmentFunction;
    pipelineStateDescriptor.vertexDescriptor = _mtlVertexDescriptor;
    pipelineStateDescriptor.colorAttachments[0].pixelFormat = view.colorPixelFormat;
    pipelineStateDescriptor.depthAttachmentPixelFormat = view.depthStencilPixelFormat;
    pipelineStateDescriptor.stencilAttachmentPixelFormat = view.depthStencilPixelFormat;

    NSError *error = NULL;
    _pipelineState = [_device newRenderPipelineStateWithDescriptor:pipelineStateDescriptor error:&error];
    if (!_pipelineState)
    {
        NSLog(@"Failed to created pipeline state, error %@", error);
    }

    MTLDepthStencilDescriptor *depthStateDesc = [[MTLDepthStencilDescriptor alloc] init];
    depthStateDesc.depthCompareFunction = MTLCompareFunctionLess;
    depthStateDesc.depthWriteEnabled = YES;
    _depthState = [_device newDepthStencilStateWithDescriptor:depthStateDesc];

    _commandQueue = [_device newCommandQueue];
    
    MTLTextureDescriptor* desc = [MTLTextureDescriptor texture2DDescriptorWithPixelFormat:MTLPixelFormatRGBA8Unorm width:kBackbufferWidth height:kBackbufferHeight mipmapped:NO];
    desc.usage = MTLTextureUsageShaderRead;
    _backbuffer = [_device newTextureWithDescriptor:desc];
    _backbufferPixels = new uint32_t[kBackbufferWidth * kBackbufferHeight];
}

static Texture* LoadTexture(const char* path)
{
    int width, height;
    unsigned char* data = stbi_load(path, &width, &height, NULL, 4);
    if (!data)
    {
        char buf[200];
        getcwd(buf, 200);
        printf("Failed to load texture %s curdir %s\n", path, buf);
        return NULL;
    }
    Texture* tex = new Texture(width, height);
    memcpy(tex->Data(), data, width*height*4);
    stbi_image_free(data);
    return tex;
}


- (void)_loadAssets
{
    NSError *error;
    MTKMeshBufferAllocator *metalAllocator = [[MTKMeshBufferAllocator alloc]
                                              initWithDevice: _device];
    MDLMesh *mdlMesh = [MDLMesh newPlaneWithDimensions:(vector_float2){2, 2}
                                            segments:(vector_uint2){1, 1}
                                        geometryType:MDLGeometryTypeTriangles
                                           allocator:metalAllocator];
    MDLVertexDescriptor *mdlVertexDescriptor =
    MTKModelIOVertexDescriptorFromMetal(_mtlVertexDescriptor);
    mdlVertexDescriptor.attributes[0].name  = MDLVertexAttributePosition;
    mdlVertexDescriptor.attributes[1].name  = MDLVertexAttributeTextureCoordinate;
    mdlMesh.vertexDescriptor = mdlVertexDescriptor;

    _mesh = [[MTKMesh alloc] initWithMesh:mdlMesh device:_device error:&error];
    if(!_mesh || error)
    {
        NSLog(@"Error creating MetalKit mesh %@", error.localizedDescription);
    }

    Texture* texScope = LoadTexture("../Cs/Unity/Assets/Data/Scope.png");
    Texture* texView = LoadTexture("../Cs/Unity/Assets/Data/View.png");
    
    InitializeStuff(texScope, texView);
}

- (void)_updateBackbufferState
{
    static int frameCounter = 0;
    static uint64_t frameTime = 0;
    uint64_t time1 = mach_absolute_time();
    
    uint64_t curNs = (time1 * _clock_timebase.numer) / _clock_timebase.denom;
    float curT = float(curNs * 1.0e-9f);

    DrawStuff(curT, kBackbufferWidth, kBackbufferHeight, (Color*)_backbufferPixels);
    
    uint64_t time2 = mach_absolute_time();
    ++frameCounter;
    frameTime += (time2-time1);
    if (frameCounter > 10)
    {
        uint64_t ns = (frameTime * _clock_timebase.numer) / _clock_timebase.denom;
        float s = (float)(ns * 1.0e-9) / frameCounter;
        printf("%.2fms (%.1f FPS)\n", s * 1000.0f, 1.f / s);
        frameCounter = 0;
        frameTime = 0;
    }
    
    [_backbuffer replaceRegion:MTLRegionMake2D(0,0,kBackbufferWidth,kBackbufferHeight) mipmapLevel:0 withBytes:_backbufferPixels bytesPerRow:kBackbufferWidth*4];
}

- (void)drawInMTKView:(nonnull MTKView *)view
{
    dispatch_semaphore_wait(_inFlightSemaphore, DISPATCH_TIME_FOREVER);

    id <MTLCommandBuffer> commandBuffer = [_commandQueue commandBuffer];
    commandBuffer.label = @"MyCommand";

    __block dispatch_semaphore_t block_sema = _inFlightSemaphore;
    [commandBuffer addCompletedHandler:^(id<MTLCommandBuffer> buffer)
     {
         dispatch_semaphore_signal(block_sema);
     }];

    [self _updateBackbufferState];

    // Delay getting the currentRenderPassDescriptor until we absolutely need it to avoid
    //   holding onto the drawable and blocking the display pipeline any longer than necessary
    MTLRenderPassDescriptor* renderPassDescriptor = view.currentRenderPassDescriptor;

    if(renderPassDescriptor != nil)
    {
        id <MTLRenderCommandEncoder> renderEncoder =
        [commandBuffer renderCommandEncoderWithDescriptor:renderPassDescriptor];
        renderEncoder.label = @"MyRenderEncoder";

        [renderEncoder pushDebugGroup:@"DrawBox"];

        [renderEncoder setFrontFacingWinding:MTLWindingCounterClockwise];
        [renderEncoder setCullMode:MTLCullModeNone];
        [renderEncoder setRenderPipelineState:_pipelineState];
        [renderEncoder setDepthStencilState:_depthState];

        for (NSUInteger bufferIndex = 0; bufferIndex < _mesh.vertexBuffers.count; bufferIndex++)
        {
            MTKMeshBuffer *vertexBuffer = _mesh.vertexBuffers[bufferIndex];
            if((NSNull*)vertexBuffer != [NSNull null])
            {
                [renderEncoder setVertexBuffer:vertexBuffer.buffer
                                        offset:vertexBuffer.offset
                                       atIndex:bufferIndex];
            }
        }

        [renderEncoder setFragmentTexture:_backbuffer atIndex:0];

        for(MTKSubmesh *submesh in _mesh.submeshes)
        {
            [renderEncoder drawIndexedPrimitives:submesh.primitiveType
                                      indexCount:submesh.indexCount
                                       indexType:submesh.indexType
                                     indexBuffer:submesh.indexBuffer.buffer
                               indexBufferOffset:submesh.indexBuffer.offset];
        }
        [renderEncoder popDebugGroup];
        [renderEncoder endEncoding];
        [commandBuffer presentDrawable:view.currentDrawable];
    }
    [commandBuffer commit];
}

- (void)mtkView:(nonnull MTKView *)view drawableSizeWillChange:(CGSize)size
{
}

@end
